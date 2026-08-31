#include "pico/stdlib.h"
#include "hardware/dma.h"
#include "hardware/pio.h"
#include "pio_ltc264x.h"
#include "file_player.h"
#include "multi_transfer_manager.h"
#include "f_util.h"
#include "ff.h"
#include "hw_config.h"
#include <array>
#include <cstdio>
#include <ranges>

using T = uint16_t;
inline constexpr size_t NUM_FILES = 4;
inline constexpr size_t SD_CHUNK_SIZE = 32768;  // must be factor of 512.
inline constexpr size_t BUF_SIZE = SD_CHUNK_SIZE/sizeof(T);

std::array<const char*, NUM_FILES> filenames
{{"channel_0.bin", "channel_1.bin", "channel_2.bin", "channel_3.bin"}};

struct DACPins
{
    uint32_t pico;
    uint32_t sck;
    uint32_t cs;
};

std::array<DACPins, NUM_FILES> DAC_PINS
{{
    {.pico = 4, .sck = 5, .cs = 6},
    {.pico = 8, .sck = 9, .cs = 10},
    {.pico = 12, .sck = 13, .cs = 14},
    {.pico = 16, .sck = 17, .cs = 18}
}};

/* SDIO Interface */
static sd_sdio_if_t sdio_if = {
//  CLK_gpio = D0_gpio - 2; -> derived from D0_gpio.
    .CMD_gpio = 22,
    .D0_gpio = 23,
//    D1_gpio = D0_gpio + 1; -> derived from D0_gpio.
//    D2_gpio = D0_gpio + 2; -> derived from D0_gpio.
//    D3_gpio = D0_gpio + 3; -> derived from D0_gpio.
    .SDIO_PIO = pio0,
    .DMA_IRQ_num = DMA_IRQ_0,
    .use_exclusive_DMA_IRQ_handler = true,
    .baud_rate = 150 * 1000 * 1000 / 6, // RP2350: */5 -> 30000000 Hz
                                        // RP2350: */6 -> 25000000 Hz
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
 * @param[in] num The number of the SD card to get.
 * @return A pointer to the SD card object, or @c NULL if the number is invalid.
 */
sd_card_t* sd_get_by_num(size_t num) {
    if (0 == num)
    {return &sd_card;}
    else
    {return NULL;}
}

    //extern MultiFilePlayer<T, NUM_FILES, SD_CHUNK_SIZE/sizeof(T)> player;

int main() {
    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    // Mount SD card.
    FATFS fs;
    FRESULT fr = f_mount(&fs, "", 1);
    if (FR_OK != fr)
    {panic("f_mount error: %s (%d)\n", FRESULT_str(fr), fr);}
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");

    // Setup PIO Block for DAC communication.
    std::array<PIO_LTC264x, NUM_FILES> dacs
    {{{pio2, DAC_PINS[0].sck, DAC_PINS[0].pico},
      {pio2, DAC_PINS[1].sck, DAC_PINS[1].pico, false, dacs[0].get_offset()},
      {pio2, DAC_PINS[2].sck, DAC_PINS[2].pico, false, dacs[0].get_offset()},
      {pio2, DAC_PINS[3].sck, DAC_PINS[3].pico, false, dacs[0].get_offset()}
    }};
    for (const auto& dac: dacs)
        dac.start();

    // Create buffers.
    std::array<TimerPacedDMADoubleBuffer<T, BUF_SIZE>, NUM_FILES> bufs
    {{
        {dacs[0].get_tx_fifo_address()},
        {dacs[1].get_tx_fifo_address()},
        {dacs[2].get_tx_fifo_address()},
        {dacs[3].get_tx_fifo_address()}
    }};
    // Create array of buffer ptrs for MultiTransferManager.
    std::array<DMADoubleBuffer<T, BUF_SIZE>*, NUM_FILES> buf_ptrs
    {{ &bufs[0], &bufs[1], &bufs[2], &bufs[3] }};

    // Create file players. Take default file playback settings.
    std::array<FilePlayer<T, BUF_SIZE>, NUM_FILES> players
    {{ {}, {}, {}, {} }};

    // Setup File Players.
    printf("FilePlayers claiming buffers and opening respective files.\r\n");
    for (auto [index, player] : std::views::enumerate(players))
    {
        player.claim_buffer(&bufs[index]);
        player.open_file(filenames[index]); // will preload buffers.
    }
    printf("All FilePlayers ready.\r\n");
    sleep_ms(500);

    // Create MultiTransferManager
    printf("Setting up MultiTransferManager.\r\n");
    sleep_ms(500);
    MultiTransferManager<T, BUF_SIZE, NUM_FILES> transfer_manager(buf_ptrs, dacs);
    transfer_manager.reset();
    transfer_manager.enable_end_of_transfer_interrupt(1); // corresponds to DMA_IRQ_1

    printf("Starting multiple channels.\r\n");
    transfer_manager.start(0b1111);
    uint32_t busy_players = 0b1111;
    while (true)
    {
        for (auto [index, player]: std::views::enumerate(players))
        {
            if (player.is_busy())
                player.update();
            else // Mark this player as finished.
                busy_players &= ~(1u << index);
        }
        if (!busy_players) // exit if all players are finished.
            break;
    }
    printf("Done playing!\r\n");
    end_of_transfer_event_t event;
    while (transfer_manager.get_finished_transfers(&event))
    {
        printf("Got end-of-transfer-event: (0b%032b, %llu [us])\r\n",
               event.finished_channels_mask, event.timestamp_us);
    }
    sleep_ms(1000);

    // Cleanup
    for (auto& player: players)
        player.close_file();

    // Unmount the file system.
    f_unmount("");
    printf("All transfers complete! Goodbye, world!\r\n");
    for (;;);
}
