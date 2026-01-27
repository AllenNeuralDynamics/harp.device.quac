#include <stdio.h>
#include <pico/stdlib.h>
#include <dma_double_buffer.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
#include <pio_ltc264x.h>
#include "f_util.h"
#include "ff.h"
#include "hw_config.h"

inline constexpr uint32_t PAUSE_INTERVAL_US = 500000;
inline constexpr uint32_t WORD_COUNT = 32768;

using T = uint16_t;

#define PICO_PIN (15)
#define SCK_PIN (16)
#define CS_PIN (17)

int main() {
    UINT bytes_read;

    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");

    // Setup PIO Block for DAC communication.
    PIO_LTC264x dac(pio2, SCK_PIN, PICO_PIN);
    dac.start();

    // Setup transfer rate for 500K words-per-sec. Assume soure clock of 150MHz.
    int dma_timer_chan = dma_claim_unused_timer(true);
    printf("Claimed DMA Timer %d.\r\n", dma_timer_chan);
    dma_timer_set_fraction(dma_timer_chan, 1, 30000); // 5 KHz
    dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan));

    // Create double-buffer. Note: buffer is sized in T-size, not byte size.
    DMADoubleBuffer<T, WORD_COUNT> buf(pacing_signal, &pio2->txf[dac.get_sm()]);

    // Copy of the current idle buffer name so we can track when it toggles.
    T* idle_buffer = nullptr;
    printf("Starting transfer.\r\n");
    buf.setup_last_dma_transfer(WORD_COUNT); // only do one buffer transfer.
    buf.start_transfer();
    uint32_t start_time_s = time_us_32();
    uint32_t next_time_us = start_time_s + PAUSE_INTERVAL_US;
    while (true)
    {
        while (int32_t(time_us_32() - next_time_us) < 0){}
        if (buf.transfer_complete())
            break;

        next_time_us += PAUSE_INTERVAL_US;
        buf.pause_transfer();
        printf("Paused transfer.\r\n");
        if (buf.is_paused())
            printf("Transfer is now paused.\r\n");
        else
            printf("ERROR: Transfer not paused or not detected as paused.\r\n");

        while (int32_t(time_us_32() - next_time_us) < 0){}

        next_time_us += PAUSE_INTERVAL_US;
        buf.resume_transfer();
        printf("Resumed transfer.\r\n");
        if (!buf.is_paused() || buf.transfer_complete())
            printf("Transfer is now resumed.\r\n");
        else
            printf("ERROR: Transfer not resumed or not detected as resumed.\r\n");
    }
    printf("Transfer complete! Goodbye, world.\r\n");
    for (;;);
}
