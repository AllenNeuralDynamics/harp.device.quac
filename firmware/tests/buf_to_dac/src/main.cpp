#include <stdio.h>
#include <pico/stdlib.h>
#include <cmath>
#include <dma_double_buffer.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
//#include <pio_ltc264x.h>

#define PICO_PIN (15)
#define SCK_PIN (16)
#define CS_PIN (17)


//static constexpr size_t BUFFER_SIZE = 512*64; // 32768 words
//static constexpr size_t BUFFER_SIZE = 512*32; // 32768 bytes
static constexpr size_t BUFFER_SIZE = 512*32; // 32768 bytes

int main() {
    uint16_t short_sink; // temporary place to dump the data.
    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");
    //PIO_LTC264x dac(pio0, SCK_PIN, PICO_PIN); // CS pin is <SCK pin> + 1

    // Setup transfer rate for 500K words-per-sec. Assume soure clock of 150MHz.
    int dma_timer_chan = dma_claim_unused_timer(true);
    printf("Claimed DMA Timer %d.\r\n", dma_timer_chan);
    dma_timer_set_fraction(dma_timer_chan, 1, 300); // numerator=1, denominator=300
    dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan));

    // Create double-buffer.
    DMADoubleBuffer<uint16_t, BUFFER_SIZE> file_buf(pacing_signal, &short_sink);
    //In practice it will be: &pio0->txf[0]);

    // Load starting buffer with data.
    uint16_t idle_buffer_id = file_buf.get_idle_buffer_id();
    printf("Loading buffer with first block of data into buffer[%d].\r\n", idle_buffer_id);
    uint16_t(*idle_buffer)[BUFFER_SIZE] = file_buf.get_idle_buffer();
    //for (uint16_t i = 0 ; i < BUFFER_SIZE; ++i)
    //    (*idle_buffer)[i] = i;

    // Kick off the transfer.
    printf("Starting transfer.\r\n");
    file_buf.start_transfer();

    for (size_t i = 0; i < 5; ++i)
    {
        // Load open buffer with data while the busy buffer transfers out.
        idle_buffer_id = file_buf.get_idle_buffer_id();
        printf("Loading idle buffer (buffer[%d]).\r\n", idle_buffer_id);
        idle_buffer = file_buf.get_buffer(idle_buffer_id);
        //for (uint16_t i = 0 ; i < BUFFER_SIZE; ++i)
        //    (*idle_buffer)[i] = i;

        // Wait for buffer-switch.
        printf("Waiting for buffer to switch.\r\n");
        while (idle_buffer_id == file_buf.get_idle_buffer_id()){}
    }

    // Setup final transfer. (We can use f_eof in practice.)
    printf("Setting up last block transfer.\r\n");
    size_t last_transfer_num_words = std::ceil(BUFFER_SIZE/2);// arbitrary value.
    file_buf.setup_last_dma_transfer(last_transfer_num_words);
    idle_buffer_id = file_buf.get_idle_buffer_id();
    idle_buffer = file_buf.get_buffer(idle_buffer_id);
    //for (uint16_t i = 0 ; i < last_transfer_num_words; ++i)
    //    (*idle_buffer)[i] = i;

    // Wait for transfer to complete.
    while (!file_buf.transfer_complete()){}

    // Thats it!
    printf("Transfer Complete! Goodbye, world!\r\n");
    for (;;);
}
