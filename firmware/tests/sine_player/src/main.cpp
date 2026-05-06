#include "pico/stdlib.h"
#include "hardware/dma.h"
#include "hardware/pio.h"
#include "pio_ltc264x.h"
#include "dma_double_buffer.h"
#include "waveform_settings.h"
#include "sine_wave_player.h"
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
    PIO& pio = dac.get_pio(); // TODO: dac.get_tx_fifo()
    int32_t sm = dac.get_sm();
    TimerPacedDMADoubleBuffer<T, BUF_SIZE> buf(&pio->txf[sm]);
    // Create SinePlayer
    SineWavePlayer<T, BUF_SIZE> player;
    FunctionSettings settings;
    settings.duration_us = 10000000u;
    settings.update_frequency_hz = 500000u;
    player.claim_buffer(&buf);
    player.apply_settings(settings);
    player.setup();
    printf("Sine Player is ready.\r\n");
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
