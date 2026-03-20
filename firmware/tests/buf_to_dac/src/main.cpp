#include <stdio.h>
#include <pico/stdlib.h>
#include <cmath>
#include <dma_double_buffer.h>
#include <hardware/dma.h>
#include <hardware/pio.h>
#include <pio_ltc264x.h>

#define PICO_PIN (4)
#define SCK_PIN (5)
#define CS_PIN (6)

inline constexpr size_t LED0_PIN = 2;
inline constexpr size_t LED1_PIN = 3;
inline constexpr uint32_t LED_MASK = (1u << LED0_PIN) | (1u << LED1_PIN);

inline constexpr size_t BUFFER_SIZE = 512*32;

void irq_handler()
{
    dma_hw->ints0 = dma_hw->ints0; // Clear interrupt status.
    gpio_put_masked(LED_MASK, 0xFFFFFFFF);
}

int main() {
    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");

    gpio_init_mask(LED_MASK);
    gpio_set_dir_masked(LED_MASK, 0xFFFFFFFF);
    gpio_put_masked(LED_MASK, 0);

    PIO_LTC264x dac(pio0, SCK_PIN, PICO_PIN); // CS pin is <SCK pin> + 1
    dac.start();

    // Setup transfer rate for 500K words-per-sec. Assume soure clock of 150MHz.
    int dma_timer_chan = dma_claim_unused_timer(true);
    printf("Claimed DMA Timer %d.\r\n", dma_timer_chan);
    dma_timer_set_fraction(dma_timer_chan, 1, 300); // numerator=1, denominator=300
    dreq_num_t pacing_signal = dreq_num_t(dma_get_timer_dreq(dma_timer_chan));

    int irq_index = 0;

    // Create double-buffer.
    using T = uint16_t;
    DMADoubleBuffer<T, BUFFER_SIZE> file_buf(pacing_signal, &pio0->txf[dac.get_sm()]);
    file_buf.enable_end_of_transfer_irq(irq_index);
    //In practice it will be: &pio0->txf[<state_machine_number]);
    // FIXME: we need to deal with 16-bit transfers, but the library accepts 32-bit-wide inputs.

    irq_set_exclusive_handler(DMA_IRQ_0 + irq_index, irq_handler);
    irq_set_enabled(DMA_IRQ_0 + irq_index, true);

    //printf("double buffer[0][]: %p\r\n", file_buf.buffers_[0]);
    //printf("double buffer[1][]: %p\r\n", file_buf.buffers_[1]);
    //printf("ctrl_chan_data_[0]: %p\r\n", file_buf.ctrl_chan_data_[0]);
    //printf("ctrl_chan_data_[1]: %p\r\n", file_buf.ctrl_chan_data_[1]);
    //printf("ctrl_chan read addr (as pointer): %p\r\n", dma_channel_hw_addr(file_buf.ctrl_chan_)->read_addr);

    // Load starting buffer with data.
    T* idle_buffer = file_buf.get_idle_buffer();
    printf("Loading buffer with first block of data into buffer@%p.\r\n", idle_buffer);
    for (T i = 0 ; i < BUFFER_SIZE; ++i)
    {
        idle_buffer[i] = i;
    }

    printf("Starting transfer.\r\n");
    file_buf.start_transfer();

    for (size_t i = 0; i < 5; ++i)
    {
        // Load open buffer with data while the busy buffer transfers out.
        // We assume we can fill the buffer fast than it will be written out.
        idle_buffer = file_buf.get_idle_buffer();
        printf("Loading idle buffer@%p.\r\n", idle_buffer);
        for (T j = 0 ; j < BUFFER_SIZE; ++j)
        {
            if (i%2 == 0)
                idle_buffer[j] = 0x0000;
            else
                idle_buffer[j] = 0xFFFF;
        }
        // Wait for buffer-switch.
        printf("Waiting for buffer to switch.\r\n");
        while (idle_buffer == file_buf.get_idle_buffer()){}
    }

    // Setup final transfer. (We can use f_eof in practice.)
    printf("Setting up last block transfer.\r\n");
    size_t last_transfer_num_words = std::ceil(BUFFER_SIZE/2);// arbitrary value.
    file_buf.setup_last_dma_transfer(last_transfer_num_words);
    idle_buffer = file_buf.get_idle_buffer();
    printf("Loading idle buffer@%p.\r\n", idle_buffer);
    //for (T i = 0 ; i < last_transfer_num_words; ++i)
    //    idle_buffer[i] = i;
    // Wait for final transfer to kick off and complete.
    while (!file_buf.transfer_complete()){}

    // Thats it!
    printf("Transfer Complete! Goodbye, world!\r\n");
    for (;;);
}
