#include "pico/stdlib.h"
#include "pio_ltc264x.h"
#include <array>
#include <cstdint>


inline constexpr size_t NUM_CHANNELS = 4;

struct DACPins
{
    uint32_t pico;
    uint32_t sck;
    uint32_t cs;
};

inline constexpr std::array<DACPins, NUM_CHANNELS> DAC_PINS
{{
    {.pico = 4, .sck = 5, .cs = 6},
    {.pico = 7, .sck = 8, .cs = 9},
    {.pico = 10, .sck = 11, .cs = 12},
    {.pico = 13, .sck = 14, .cs = 15}
}};

std::array<PIO_LTC264x, NUM_CHANNELS> dacs
{{
    {pio2, DAC_PINS[0].sck, DAC_PINS[0].pico},
    {pio2, DAC_PINS[1].sck, DAC_PINS[1].pico, false, dacs[0].get_offset()},
    {pio2, DAC_PINS[2].sck, DAC_PINS[2].pico, false, dacs[0].get_offset()},
    {pio2, DAC_PINS[3].sck, DAC_PINS[3].pico, false, dacs[0].get_offset()},
}};

int main()
{
    for (const auto& dac: dacs)
        dac.start();
    uint16_t i = 0;
    while (true)
    {
        for (auto& dac: dacs)
        {
            // Write unique values per dac.
            dac.write_value(i);
            ++i;
        }
        sleep_ms(1);
    }
}
