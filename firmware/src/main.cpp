#include <array>
#include <cstring>
#include "core1_file_player.h"
#include "dma_double_buffer.h"
#include "file_player.h"
#include "sine_wave_player.h"
#include "trapezoid_player.h"
#include "multi_transfer_manager.h"
#include "harp_c_app.h"
#include "harp_synchronizer.h"
#include "config.h"
#include "quac_app.h"
#include "pio_ltc264x.h"
#include "pico/multicore.h"
#ifdef DEBUG
    #include <cstdio> // for printf
    #include "pico/stdlib.h" // for uart printing
#endif

const uint8_t interface_hash[20] = INTERFACE_HASH;

// Create Harp App.
HarpCApp& app = HarpCApp::init(HARP_DEVICE_ID, FW_VERSION, HW_VERSION, "quac",
                               (uint8_t*)GIT_HASH, interface_hash,
                               app_reg_specs, APP_REG_COUNT,
                               update_app, reset_app);

std::array<PIO_LTC264x, NUM_CHANNELS> dacs
{{
    {pio2, DAC_PINS[0].sck, DAC_PINS[0].pico},
    {pio2, DAC_PINS[1].sck, DAC_PINS[1].pico, false, dacs[0].get_offset()},
    {pio2, DAC_PINS[2].sck, DAC_PINS[2].pico, false, dacs[0].get_offset()},
    {pio2, DAC_PINS[3].sck, DAC_PINS[3].pico, false, dacs[0].get_offset()},
}};

std::array<FilePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> file_players{};
std::array<SinePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> sine_players{};
std::array<TrapezoidPlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> trapezoid_players{};

std::array<TimerPacedDMADoubleBuffer<T, READ_BUF_SIZE>, NUM_CHANNELS> bufs
{{
    {dacs[0].get_tx_fifo_address()},
    {dacs[1].get_tx_fifo_address()},
    {dacs[2].get_tx_fifo_address()},
    {dacs[3].get_tx_fifo_address()}
}};

std::array<DMADoubleBuffer<T, READ_BUF_SIZE>*, NUM_CHANNELS> buf_ptrs
{{ &bufs[0], &bufs[1], &bufs[2], &bufs[3] }};

/// Aggregate for iterating overall all players via a base class pointer
std::array<SourcePlayer<T, READ_BUF_SIZE>*, NUM_CHANNELS * NUM_PLAYER_TYPES>
player_ptrs;

MultiTransferManager<T, READ_BUF_SIZE, NUM_CHANNELS> transfer_manager(buf_ptrs,
                                                                      dacs);

void set_led(bool enabled)
{    gpio_put(DEBUG_LEDS[0], enabled);}

bool get_led()
{    return gpio_get(DEBUG_LEDS[0]);}

// Core0 main.
int main()
{
// Configure OP_LED
    gpio_init(DEBUG_LEDS[0]);
    gpio_set_dir(DEBUG_LEDS[0], GPIO_OUT);
    gpio_put(DEBUG_LEDS[0], 0);
// Init Synchronizer.
    HarpSynchronizer::init(uart1, HARP_SYNC_RX_PIN);
    app.set_synchronizer(&HarpSynchronizer::instance());
    app.set_op_led_fns(set_led, get_led);
#ifdef DEBUG
    stdio_uart_init_full(uart0, 921600, UART_TX_PIN, -1); // use uart1 tx only.
    printf("Hello, from the quac board!\r\n");
#endif
    queue_init(&ext_trigger_event_queue, sizeof(ext_trigger_event_t), 32);
    // Populate player_ptrs.
    size_t index = 0;
    for (auto& file_player: file_players) player_ptrs[index++] = &file_player;
    for (auto& sine_player: sine_players) player_ptrs[index++] = &sine_player;
    for (auto& trapz_player: trapezoid_players) player_ptrs[index++] = &trapz_player;
    // Mount the file system.
    FATFS fs;
    FRESULT fr = f_mount(&fs, "", 1);
    // Setup DACs first.
    for (const auto& dac: dacs)
        dac.start();
    reset_app();
    while (true)
        app.run();
}
