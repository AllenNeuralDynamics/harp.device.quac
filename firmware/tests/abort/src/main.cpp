#include <stdio.h>
#include <pico/stdlib.h>
#include <dma_double_buffer.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
#include <pio_ltc264x.h>
#include "f_util.h"
#include "ff.h"
#include "hw_config.h"

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
    printf("Starting transfer.\r\n");
    buf.setup_last_dma_transfer(WORD_COUNT); // only do one buffer transfer.
    buf.start_transfer();
    // Wait for transfer to start. (Should be instant.)
    printf("Waiting for transfer to start.\r\n");
    while (!buf.is_transferring()){}
    sleep_ms(1);
    buf.abort_transfer();
    if (!buf.is_transferring())
        printf("Transfer aborted.\r\n");
    else
        printf("ERROR: Transfer not aborted or not detected as such.\r\n");
    printf("Transfer complete! Goodbye, world.\r\n");
    for (;;);
}
