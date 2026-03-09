
#include <stdio.h>
#include "pico/stdlib.h"
#include "f_util.h"
#include "ff.h"
#include "hw_config.h"
#include <cmath>


static constexpr size_t NUM_FILES = 4;
static constexpr size_t DATA_NUM_WORDS = 5'000'000;
static constexpr size_t DATA_NUM_BYTES = DATA_NUM_WORDS * 2;
static constexpr size_t BLOCK_SIZE = 32768;  // must be factor of 512.


/* SDIO Interface */
static sd_sdio_if_t sdio_if = {
    /*
    Pins CLK_gpio, D1_gpio, D2_gpio, and D3_gpio are at offsets from pin D0_gpio.
    The offsets are determined by sd_driver\SDIO\rp2040_sdio.pio.
        CLK_gpio = (D0_gpio + SDIO_CLK_PIN_D0_OFFSET) % 32;
        As of this writing, SDIO_CLK_PIN_D0_OFFSET is 30,
            which is -2 in mod32 arithmetic, so:
    */

//  CLK_gpio = D0_gpio - 2; -> derived from D0_gpio.
    .CMD_gpio = 22,//3,
    .D0_gpio = 23,//4,
//    D1_gpio = D0_gpio + 1; -> derived from D0_gpio.
//    D2_gpio = D0_gpio + 2; -> derived from D0_gpio.
//    D3_gpio = D0_gpio + 3; -> derived from D0_gpio.
    .baud_rate = 150 * 1000 * 1000 / 5  // RP2350: */8 -> 18750000 Hz.
                                        // RP2350: */5 -> 30000000 Hz
                                        // RP2040: */6 -> 20833333 Hz
};

/* Hardware Configuration of the SD Card socket "object" */
static sd_card_t sd_card = {.type = SD_IF_SDIO, .sdio_if_p = &sdio_if};

/**
 * @brief Get the number of SD cards.
 *
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
    if (0 == num) {
        // The number 0 is a valid SD card number.
        // Return a pointer to the sd_card object.
        return &sd_card;
    } else {
        // The number is invalid. Return @c NULL.
        return NULL;
    }
}

alignas(16) uint8_t __not_in_flash("buffers") buffer[NUM_FILES][BLOCK_SIZE]; /* File read buffer */
FIL __not_in_flash("file_handlers") fil[NUM_FILES];

/**
 * @brief The main function of the program.
 *
 * @details This function initializes the stdio interface, prints a greeting to the
 * console, mounts the SD card, writes a message to a file, and unmounts the SD card.
 *
 */
int main() {
    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");

    // Mount SD card.
    FATFS fs;
    FRESULT fr = f_mount(&fs, "", 1);
    if (FR_OK != fr)
    {panic("f_mount error: %s (%d)\n", FRESULT_str(fr), fr);}

    //FIL fil[NUM_FILES];
    //alignas(16) uint8_t buffer[NUM_FILES][BLOCK_SIZE]; /* File read buffer */
    UINT bytes_read;        /* Bytes read */
    const char* const filename[] = {"channel_0.txt", "channel_1.txt",
                                    "channel_2.txt", "channel_3.txt"};
    // Open 4 files. Note: max number is set by FF_FS_LOCK in ffconf.h
    for (size_t file_index = 0; file_index < NUM_FILES; ++file_index)
    {
        fr = f_open(&fil[file_index], filename[file_index], FA_READ);
        if (fr != FR_OK)
            {panic("Could not open: %s", filename);}
    }
    uint32_t start_time_us = time_us_32();
    // Read the data in chunks.
    size_t offset = 0;
    for (size_t block_index = 0; block_index < ceil(DATA_NUM_BYTES/BLOCK_SIZE); ++block_index)
    {
        for (size_t file_index = 0; file_index < NUM_FILES; ++file_index)
        {
            // Seek to the new location to read from.
            //fr = f_lseek(&fil[file_index], offset); // TODO: we don't need this?
            // Read the data to the buffer.
            fr = f_read(&fil[file_index], buffer[file_index], BLOCK_SIZE, &bytes_read);
            //printf("Read %d bytes starting from address %d, from %s. ",
            //        bytes_read, offset, filename[file_index]);
            //printf("First few uint16s per block: ");
            //uint16_t* buffer_as_uint16s = reinterpret_cast<uint16_t*>(buffer[file_index]);
            //for (size_t i = 0; i < 8; ++i)
            //    {printf("%d ", buffer_as_uint16s[i]);}
            //printf("\r\n");
            if (fr != FR_OK)
                {panic("Could not read the data: %s", filename);}
        }
        //offset+= BLOCK_SIZE; // TODO: we don't need this?
        //sleep_ms(1000); // TODO: remove this.
    }
    uint32_t fin_time_us = time_us_32();
    printf("Total time to read all %d waveforms: %d [us].\r\n",
        NUM_FILES, fin_time_us - start_time_us);
    // Close 4 files.
    for (size_t file_index = 0; file_index < NUM_FILES; ++file_index)
    {
        fr = f_close(&fil[file_index]);
        if (fr != FR_OK)
            {panic("Could not close: %s", filename);}
    }

    f_unmount("");
    printf("Finished reading!\r\n");
    for (;;);
}
