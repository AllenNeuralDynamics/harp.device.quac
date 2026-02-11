#include <hw_config.h> // from no-os* sd card library.
#include <config.h>

static sd_sdio_if_t SD_SDIO_INTERFACE
{
    // Pins CLK_gpio, D1_gpio, D2_gpio, and D3_gpio are relative to pin D0_gpio.

//  CLK_gpio = D0_gpio - 2; -> derived from D0_gpio.
    .CMD_gpio = SD_CMD_PIN,
    .D0_gpio = SD_D0_PIN,
//    D1_gpio = D0_gpio + 1; -> derived from D0_gpio.
//    D2_gpio = D0_gpio + 2; -> derived from D0_gpio.
//    D3_gpio = D0_gpio + 3; -> derived from D0_gpio.
    .SDIO_PIO = SD_PIO,
    .baud_rate = SD_READ_SPEED_HZ
                                        // RP2040: */6 -> 20833333 Hz
};

static sd_card_t SDCard
{
    .type = SD_IF_SDIO,
    .sdio_if_p = &SD_SDIO_INTERFACE // interface spec from config.h
};

// Function implementations for definitions in hw_config.h.
// These must be defined for the fatfs calls to work per this project's spec.

size_t sd_get_num()
{return 1;}

sd_card_t* sd_get_by_num(size_t num)
{
    if (num == 0)
        return &SDCard;
    return nullptr;
}
