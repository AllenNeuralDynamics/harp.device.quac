#include <cstring>
#include <harp_c_app.h>
#include <harp_synchronizer.h>
#include <config.h>
#include <quac_app.h>
#include <pio_ltc264x.h>
#ifdef DEBUG
    #include <pico/stdlib.h> // for uart printing
    #include <cstdio> // for printf
#endif


// Create Harp App.
HarpCApp& app = HarpCApp::init(HARP_DEVICE_ID,
                               HW_VERSION_MAJOR, HW_VERSION_MINOR,
                               HW_ASSEMBLY_VERSION,
                               HARP_VERSION_MAJOR, HARP_VERSION_MINOR,
                               FW_VERSION_MAJOR, FW_VERSION_MINOR,
                               UNUSED_SERIAL_NUMBER,
                               "quac",
                               (uint8_t*)GIT_HASH,
                               &app_regs, app_reg_specs,
                               reg_handler_fns, APP_REG_COUNT, update_app,
                               reset_app);

std::array<PIO_LTC264x, NUM_CHANNELS> dacs
{{
    {pio2, DAC_PINS[0].sck, DAC_PINS[0].pico},
    {pio2, DAC_PINS[1].sck, DAC_PINS[1].pico, false, dacs[0].get_offset()},
    {pio2, DAC_PINS[2].sck, DAC_PINS[2].pico, false, dacs[0].get_offset()},
    {pio2, DAC_PINS[3].sck, DAC_PINS[3].pico, false, dacs[0].get_offset()},
}};
MultiFilePlayer<T, NUM_CHANNELS, READ_BUF_SIZE> player(dacs, filenames);

// Core0 main.
int main()
{
// Init Synchronizer.
    HarpSynchronizer::init(uart1, HARP_SYNC_RX_PIN);
    app.set_synchronizer(&HarpSynchronizer::instance());
#ifdef DEBUG
    stdio_uart_init_full(uart0, 921600, UART_TX_PIN, -1); // use uart1 tx only.
    printf("Hello, from the quac board!\r\n");
#endif
    queue_init(&ext_trigger_event_queue, sizeof(ext_trigger_event_t), 32);
    // Setup DACs first.
    for (const auto& dac: dacs)
        dac.start();
    reset_app();
    while (true)
        app.run();
}
