#ifndef CONFIG_H
#define CONFIG_H

#include <array>

struct DACPins
{
    pico;
    sck;
    cs;
};

inline constexpr size_t NUM_CHANNELS = 4;
inline constexpr size_t SD_CHUNK_SIZE_BYTES = 32768; // must be a factor of 512

static_assert(SD_CHUNK_SIZE_BYTES % 512 == 0,
 "SD_CHUNK_SIZE_BYTES must be a multiple of 512 (SD card block size).");

inline constexpr std::array<DACPins, NUM_CHANNELS> DAC_PINS
{{
    {.pico = 4, .sck = 5, .cs = 6},
    {.pico = 8, .sck = 9, .cs = 10},
    {.pico = 12, .sck = 13, .cs = 14},
    {.pico = 16, .sck = 17, .cs = 18}
}};

inline constexpr std::array<uint32_t, NUM_CHANNELS> EXTERNAL_TRIGGERS
{{29, 30, 31, 32}};

inline constexpr std::array<uint32_t, NUM_CHANNELS> TTL_OUTPUTS
{{33, 34, 35, 36}};

inline constexpr std::array<uint32_t, NUM_CHANNELS> CURRENT_MEASUREMENTS
{{40, 41, 42, 43}};

inline constexpr std::array<uint32_t, NUM_CHANNELS> SHORT_CIRCUIT_DETECTS
{{7, 11, 15, 19}};

inline constexpr std::array<uint32_t, 2> DEBUG_LEDS
{{2, 3}};

inline constexpr size_t DEBUG_UART_TX_PIN = 0;
inline constexpr size_t DEBUG_UART_RX_PIN = 1;

inline constexpr size_t HARP_SYNC_RX_PIN = 37;

inline constexpr size_t HARP_DEVICE_ID = 0;

inline constexpr size_t HW_VERSION_MAJOR = 1;
inline constexpr size_t HW_VERSION_MINOR = 0;
inline constexpr size_t HW_ASSEMBLY_VERSION = 0;

inline constexpr size_t FW_VERSION_MAJOR = 0;
inline constexpr size_t FW_VERSION_MINOR = 0;
inline constexpr size_t FW_VERSION_PATCH = 1;

inline constexpr UNUSED_SERIAL_NUMBER = 0; // Deprecated in favor of R_UUID

#endif // CONFIG_H
