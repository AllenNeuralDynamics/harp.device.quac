#include "pico/stdlib.h"
#include "hardware/dma.h"
#include "hardware/pio.h"
#include "pio_ltc264x.h"
#include "dma_double_buffer.h"
#include "waveform_settings.h"
#include "trapezoid_player.h"
#include <cstdint>
#include <cstdio>

using T = uint16_t;
inline constexpr size_t CHUNK_SIZE = 32768; // must be factor of 2.
inline constexpr size_t BUF_SIZE = CHUNK_SIZE / sizeof(T);

struct DACPins
{
    uint32_t pico;
    uint32_t sck;
    uint32_t cs;
};

DACPins dac_pins{.pico = 4, .sck = 5, .cs = 6};

int main() {
    stdio_init_all();
    while (!stdio_usb_connected()){ sleep_ms(100);} // Wait for user to open com port.
    printf("Hello, world, from a Raspberry Pi Pico!\r\n");
    // Setup PIO Block for DAC communication.
    PIO_LTC264x dac(pio2, dac_pins.sck, dac_pins.pico);
    dac.start();
    TimerPacedDMADoubleBuffer<T, BUF_SIZE> buf(dac.get_tx_fifo_address());
    TrapezoidPlayer<T, BUF_SIZE> player;
    TrapezoidSettings settings;
    settings.amplitude_volts = 5; // amplitude from center, not peak-to-peak.
    settings.vertical_shift_volts = 2.5;
    printf("settings:\r\n");
    printf("update_frequency_hz: %u\r\n", settings.update_frequency_hz);
    printf("amplitude: %f\r\n", settings.amplitude_volts);
    printf("duration_us: %u\r\n", settings.duration_us);
    printf("frequency_hz: %u\r\n", settings.frequency_hz);
    printf("period_us: %u\r\n", settings.period_us());
    printf("ramp_on_us: %u\r\n", settings.ramp_on_us);
    printf("ramp_off_us: %u\r\n", settings.ramp_off_us);
    printf("vshift: %f\r\n", settings.vertical_shift_volts);
    printf("\r\n");
    player.claim_buffer(&buf);
    // FYI: adjust settings before applying them.
    player.apply_settings(settings);
    player.setup();
    printf("Trapezoid Player is ready.\r\n");
    sleep_ms(500);
    printf("Starting.\r\n");
    buf.start_transfer();
    while(player.is_busy())
        player.update();
    printf("Done playing!\r\n");
    player.cleanup(); // Close file. Release buffer.
    // Unmount the file system.
    printf("All transfers complete! Goodbye, world!\r\n");
    for (;;);
}
