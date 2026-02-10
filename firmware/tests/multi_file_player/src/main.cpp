#include <stdio.h>
#include <pico/stdlib.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
#include <pio_ltc264x.h>
#include <array>
#include <f_util.h>
#include <ff.h>
#include <hw_config.h>
#include <multi_file_player.h>

using T = uint16_t;
inline constexpr size_t NUM_FILES = 4;
inline constexpr size_t SD_CHUNK_SIZE = 32768;  // must be factor of 512.

FIL __not_in_flash("file_handlers") fil[NUM_FILES];

std::array<const char*, NUM_FILES> filenames
{{"channel_0.txt", "channel_1.txt", "channel_2.txt", "channel_3.txt"}};

struct DACPins
{
    uint32_t pico;
    uint32_t sck;
    uint32_t cs;
};

std::array<DACPins, NUM_FILES> DAC_PINS
{{
    {.pico = 15, .sck = 16, .cs = 17},
    {.pico = 18, .sck = 19, .cs = 20},
    {.pico = 22, .sck = 23, .cs = 24},
    {.pico = 25, .sck = 26, .cs = 27}
}};

/* SDIO Interface */
static sd_sdio_if_t sdio_if = {
//  CLK_gpio = D0_gpio - 2; -> derived from D0_gpio.
    .CMD_gpio = 3,
    .D0_gpio = 4,
//    D1_gpio = D0_gpio + 1; -> derived from D0_gpio.
//    D2_gpio = D0_gpio + 2; -> derived from D0_gpio.
//    D3_gpio = D0_gpio + 3; -> derived from D0_gpio.
    .SDIO_PIO = pio0,
    .baud_rate = 150 * 1000 * 1000 / 5, // RP2350: */5 -> 30000000 Hz
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
    const std::array<PIO_LTC264x, NUM_FILES> dacs
    {{{pio2, DAC_PINS[0].sck, DAC_PINS[0].pico},
      {pio2, DAC_PINS[1].sck, DAC_PINS[1].pico, false, dacs[0].get_offset()},
      {pio2, DAC_PINS[2].sck, DAC_PINS[2].pico, false, dacs[0].get_offset()},
      {pio2, DAC_PINS[3].sck, DAC_PINS[3].pico, false, dacs[0].get_offset()}}};
    for (const auto& dac: dacs)
        dac.start();

    // Create MultiFilePlayer
    MultiFilePlayer<T, NUM_FILES, SD_CHUNK_SIZE/sizeof(T)> player(dacs, filenames);
    player.set_frequency_hz(500'000);
    player.setup();
    player.start(0b1111);
    while(player.is_busy())
        player.update();
    // We'll be back after a short break.
    sleep_ms(1000);
    // Retrigger all channels again.
    player.start(0b1111);
    while(player.is_busy())
        player.update();
    player.cleanup(); // Close files.
    // Unmount the file system.
    f_unmount("");
    printf("All transfers complete! Goodbye, world!\r\n");
    for (;;);
}
