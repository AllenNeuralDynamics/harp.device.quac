#include <stdio.h>
#include <pico/stdlib.h>
#include <cmath>
#include <dma_double_buffer.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
#include <pio_ltc264x.h>
#include "f_util.h"
#include "ff.h"
#include "hw_config.h"
#include <algorithm>
#include <numeric>
#include <list> // FIXME: use etl list long-term.

using T = uint16_t;

static constexpr size_t NUM_FILES = 4;
// Warning: if SD_CHUNK_SIZE is too small, then the overhead of re-accessing
// the file and reading the data will be too large such that the buffer may
// re-toggle, and you will see bogus extra transfers.
// FIXME: maybe there is a way to catch this??
static constexpr size_t SD_CHUNK_SIZE = 32768;  // must be factor of 512.

FIL __not_in_flash("file_handlers") fil[NUM_FILES];

#define PICO_PIN (15)
#define SCK_PIN (16)
#define CS_PIN (17)

#define PICO_PIN1 (18)
#define SCK_PIN1 (19)
#define CS_PIN1 (20)

#define PICO_PIN2 (22)
#define SCK_PIN2 (23)
#define CS_PIN2 (24)

#define PICO_PIN3 (25)
#define SCK_PIN3 (26)
#define CS_PIN3 (27)

/* SDIO Interface */
static sd_sdio_if_t sdio_if = {
// Pins CLK_gpio, D1_gpio, D2_gpio, and D3_gpio are at offsets from pin D0_gpio.

//  CLK_gpio = D0_gpio - 2; -> derived from D0_gpio.
    .CMD_gpio = 3,
    .D0_gpio = 4,
//    D1_gpio = D0_gpio + 1; -> derived from D0_gpio.
//    D2_gpio = D0_gpio + 2; -> derived from D0_gpio.
//    D3_gpio = D0_gpio + 3; -> derived from D0_gpio.
    .SDIO_PIO = pio0,
    .baud_rate = 150 * 1000 * 1000 / 5, // RP2350: */8 -> 18750000 Hz.
                                        // RP2350: */5 -> 30000000 Hz
                                        // RP2040: */6 -> 20833333 Hz
};

/* Hardware Configuration of the SD Card socket "object" */
static sd_card_t sd_card = {.type = SD_IF_SDIO, .sdio_if_p = &sdio_if};

/**
 * @brief Get the number of SD cards.
 * @return The number of SD cards, which is 1 in this case.
 */
size_t sd_get_num() { return 1; }

/**
 * @brief Get a pointer to an SD card object by its number.
 *
 * @param[in] num The number of the SD card to get.
 *
 * @return A pointer to the SD card object, or @c NULL if the number is invalid.
 */
sd_card_t* sd_get_by_num(size_t num) {
    if (0 == num)
    {return &sd_card;}
    else
    {return NULL;}
}

int main() {
    // Setup
    const char* const filenames[] = {"channel_0.txt", "channel_1.txt",
                                    "channel_2.txt", "channel_3.txt"};
    UINT bytes_read;

    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    // Mount SD card.
    FATFS fs;
    FRESULT fr = f_mount(&fs, "", 1);
    if (FR_OK != fr)
    {panic("f_mount error: %s (%d)\n", FRESULT_str(fr), fr);}
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");

    // Setup PIO Block for DAC communication.
    const std::array<PIO_LTC264x, NUM_FILES> dacs
    {{{pio2, SCK_PIN, PICO_PIN},
      {pio2, SCK_PIN1, PICO_PIN1, false, dacs[0].get_offset()},
      {pio2, SCK_PIN2, PICO_PIN2, false, dacs[0].get_offset()},
      {pio2, SCK_PIN3, PICO_PIN3, false, dacs[0].get_offset()}}};
    for (size_t i = 0; i < NUM_FILES; ++i)
        printf("dacs[%d]: offset_ = %d, sm = %d \r\n", i, dacs[i].get_offset(),
               dacs[i].get_sm());
    for (const auto& dac: dacs)
        dac.start();

    // Setup transfer rate for 500K words-per-sec. Assume soure clock of 150MHz.
    int dma_timer_chan = dma_claim_unused_timer(true);
    printf("Claimed DMA Timer %d.\r\n", dma_timer_chan);
    dma_timer_set_fraction(dma_timer_chan, 1, 300); // numerator=1, denominator=300
    dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan));

    // Create double-buffer. Note: buffer is sized in T-size, not byte size.
    // FIXME: file_bufs[] should resize based on NUM_FILES
    std::array<DMADoubleBuffer<T, SD_CHUNK_SIZE/sizeof(T)>, NUM_FILES> file_bufs
    {{{pacing_signal, &pio2->txf[dacs[0].get_sm()]},
      {pacing_signal, &pio2->txf[dacs[1].get_sm()]},
      {pacing_signal, &pio2->txf[dacs[2].get_sm()]},
      {pacing_signal, &pio2->txf[dacs[3].get_sm()]}}};

    // Create a trigger mask to start all Double Buffer DMA channels at once.
    int multi_channel_trigger_mask = 0;
    for (const auto& buf: file_bufs)
        multi_channel_trigger_mask |= (1u << buf.get_ctrl_channel());

    // Container to store the current idle buffer name so we can track when
    // they toggle.
    std::array<T*, NUM_FILES> idle_buffers;
    std::ranges::fill(idle_buffers, nullptr);

    // Create a list of indexes that we can prune as we finish reading files.
    std::list<size_t> file_ids;
    file_ids.resize(NUM_FILES);
    std::iota(file_ids.begin(), file_ids.end(), 0);

    // Open 4 files. Note: max number is set by FF_FS_LOCK in ffconf.h
    for (const auto& id: file_ids)
    {
        fr = f_open(&fil[id], filenames[id], FA_READ);
        if (fr != FR_OK)
            {panic("Could not open: %s", filenames[id]);}
    }
    printf("Opened %d file(s).\r\n", NUM_FILES);
    // Read the data in chunks until we reach each file's EOF. Top off the DMA channels.
    // Files can be different lengths, so we track their IDs in a linked list
    // and pop the IDs that have been fully read.
    size_t chunk_index = 0;
    while (file_ids.size())
    {
        //for (auto it = file_ids.begin(); it != file_ids.end(); ++it)
        auto it = file_ids.begin();
        while (it != file_ids.end())
        {
            size_t id = *it;
            // Get the open buffer.
            idle_buffers[id] = file_bufs[id].get_idle_buffer();
            auto& idle_buffer = idle_buffers[id];
            //printf("Loading idle buffer@%p.\r\n", idle_buffer);
            // Read the data to the buffer.
            fr = f_read(&fil[id], idle_buffer, SD_CHUNK_SIZE, &bytes_read);
            if (fr != FR_OK)
                {panic("Could not read the data: %s", filenames[id]);}
//            printf("Chunk: %d . Read %d bytes.\r\n", chunk_index, bytes_read);
//            printf("First few uint16s per block: ");
//            T* buffer_as_T = reinterpret_cast<T*>(idle_buffer);
//            for (size_t i = 0; i < 8; ++i)
//                {printf("%d ", buffer_as_T[i]);}
//            printf("\r\n");
            // Handle end-of-file. Wind down the DMA stream.
            if ((bytes_read < SD_CHUNK_SIZE) || f_eof(&fil[id]))
            {
                printf("Setting up last transfer for file %d!\r\n", id);
                // Next transfer will be the last transfer.
                file_bufs[id].setup_last_dma_transfer(bytes_read);
                // Pop the fully-read file ID from the list.
                auto next_it = std::next(it);
                file_ids.erase(it);
                it = next_it;
            }
            else
                {++it;}
        }
        // Start the transfer on our first read. TODO: pre-read next time.
        if (chunk_index == 0)
            dma_start_channel_mask(multi_channel_trigger_mask);
        // Wait for all double-buffers to swap.
        // Note: we skip this check when we setup the last transfer.
        for (const auto& id: file_ids)
        {while(idle_buffers[id] == file_bufs[id].get_idle_buffer()){}}
        ++chunk_index;
    }

    // Close all files.
    printf("Closing files.\r\n");
    for (size_t id = 0; id < NUM_FILES; ++id)
    {
        fr = f_close(&fil[id]);
        if (fr != FR_OK)
            {panic("Could not close: %s", filenames[id]);}
    }

    // Unmount.
    f_unmount("");
    printf("Finished reading!\r\n");
    // Wait for final transfer to kick off and complete.
    for (size_t id = 0; id < NUM_FILES; ++id)
    {while (!file_bufs[id].transfer_complete()){}}
    printf("All transfers complete! Goodbye, world!\r\n");
    for (;;);
}
