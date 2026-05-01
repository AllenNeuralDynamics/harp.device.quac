#include <stdio.h>
#include <pico/stdlib.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
#include <pio_ltc264x.h>
#include <array>
#include <f_util.h>
#include <ff.h>
#include <hw_config.h>
#include <file_player.h>
#include "multi_transfer_manager.h"

using T = uint16_t;
inline constexpr size_t SD_CHUNK_SIZE = 32768;  // must be factor of 512.
inline constexpr size_t BUF_SIZE = SD_CHUNK_SIZE / sizeof(T);

struct DACPins
{
    uint32_t pico;
    uint32_t sck;
    uint32_t cs;
};

DACPins dac_pins{.pico = 4, .sck = 5, .cs = 6};

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

int main() {
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
    FilePlayer<T, BUF_SIZE> player;
    // Setup dacs and buffers in an array.
    std::array<PIO_LTC264x, 1> dacs
    {{
        {pio2, dac_pins.sck, dac_pins.pico}
    }};
    std::array<TimerPacedDMADoubleBuffer<T, BUF_SIZE>, 1> bufs
    {{
        {dacs[0].get_tx_fifo_address()}
    }};
    std::array<DMADoubleBuffer<T, BUF_SIZE>*, 1> buf_ptrs
    {{
        &bufs[0]
    }};
    for (auto& dac: dacs)
        dac.start();
    MultiTransferManager<T, BUF_SIZE, 1> transfer_manager(buf_ptrs, dacs);
    transfer_manager.enable_end_of_transfer_interrupt(1); // corresponds to DMA_IRQ_1
    player.claim_buffer(&bufs[0]);
    player.open_file("channel_0.bin");
    bufs[0].set_frequency_hz(500'000);
    printf("File Player is ready.\r\n");
    sleep_ms(500);
    printf("Starting.\r\n");
    transfer_manager.start(0b0001);
    while(player.is_busy())
        player.update();
    printf("Done playing!\r\n");
    sleep_ms(1000);
    // If we did not abort, we should be able to re-trigger.
    // Retrigger all channels again.
    printf("Restarting.\r\n");
    bufs[0].start_transfer();
    while(player.is_busy())
        player.update();
    printf("Done replaying! Closing files.\r\n");
    player.cleanup(); // Close file. Release buffer.
    // Unmount the file system.
    f_unmount("");
    printf("All transfers complete! Goodbye, world!\r\n");
    for (;;);
}
