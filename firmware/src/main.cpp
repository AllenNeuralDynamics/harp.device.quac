#include <cstring>
#include <harp_c_app.h>
#include <harp_synchronizer.h>
#include <reg_types.h>
#include <config.h>
#include <pio_ltc264x.h>
#ifdef DEBUG
    #include <pico/stdlib.h> // for uart printing
    #include <cstdio> // for printf
#endif

inline constexpr size_t NUM_APP_REGS = 0;


// Define register contents.
#pragma pack(push, 1)
struct app_regs_t
{
    // TODO
} app_regs;
#pragma pack(pop)


// Define register "specs."
RegSpecs app_reg_specs[NUM_APP_REGS]
{
    // TODO
};

// Define register read-and-write handler functions.
RegFnPair reg_handler_fns[NUM_APP_REGS]
{
    // TODO
};

void app_reset()
{
    // TODO
}

void update_app()
{

}

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
                               reg_handler_fns, NUM_APP_REGS, update_app,
                               app_reset);

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

}
