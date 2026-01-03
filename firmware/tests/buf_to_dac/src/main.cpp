#include <stdio.h>
#include <pico/stdlib.h>
#include <cmath>
#include <dma_double_buffer.h>
#include <hardware/dma.h>
#include <hardware/pio.h>


static constexpr size_t BUFFER_SIZE = 512;

int main() {
    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");

    // Create double-buffer.
    DMADoubleBuffer<uint16_t, BUFFER_SIZE> file_buf;
    // Connect output to peripheral address.
    file_buf.set_target_address(&pio2->txf[0]);  // TODO: Long term get this from the LTC264x library
    // Setup transfer rate for 500K words-per-sec. Assume soure clock of 150MHz.
    int dma_timer_chan = dma_claim_unused_timer(true);
    dma_timer_set_fraction(dma_timer_chan, 1, 300); // numerator=1, denominator=300
    file_buf.connect_external_pacing_signal(dreq_num_t(DREQ_DMA_TIMER0+dma_timer_chan));

    // Load starting buffer with data.
    uint16_t(*idle_buffer)[BUFFER_SIZE] = file_buf.get_idle_buffer();
    for (uint16_t i = 0 ; i < BUFFER_SIZE; ++i)
        (*idle_buffer)[i] = i;

    // Kick off the transfer.
    file_buf.start_transfer();

    // Load open buffer with data while the busy buffer transfers out.
    uint16_t idle_buffer_id = file_buf.get_idle_buffer_id();
    idle_buffer = file_buf.get_buffer(idle_buffer_id);
    for (uint16_t i = 0 ; i < BUFFER_SIZE; ++i)
        (*idle_buffer)[i] = i;

    // Wait for buffer-switch.
    while (idle_buffer_id != file_buf.get_idle_buffer_id()){}

    // Reload open buffer with data while the busy buffer transfers out.
    idle_buffer_id = file_buf.get_idle_buffer_id();
    idle_buffer = file_buf.get_buffer(idle_buffer_id);
    for (uint16_t i = 0 ; i < BUFFER_SIZE; ++i)
        (*idle_buffer)[i] = i;

    // Wait for buffer-switch.
    while (idle_buffer_id != file_buf.get_idle_buffer_id()){}
    // Setup final transfer.
    size_t last_transfer_num_words = std::ceil(BUFFER_SIZE/2);// arbitrary value.
    file_buf.setup_last_transfer(last_transfer_num_words);
    idle_buffer_id = file_buf.get_idle_buffer_id();
    idle_buffer = file_buf.get_buffer(idle_buffer_id);
    for (uint16_t i = 0 ; i < last_transfer_num_words; ++i)
        (*idle_buffer)[i] = i;

    // Wait for transfer to complete.
    while (!file_buf.transfer_complete()){}

    // Thats it!
    printf("Transfer Complete! Goodbye, world!\r\n");
    for (;;);
}
