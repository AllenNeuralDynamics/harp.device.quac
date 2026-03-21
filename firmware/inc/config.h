#ifndef CONFIG_H
#define CONFIG_H
#include <sd_card.h>  // from no-os* sd card library.
#include <pio_ltc264x.h>
#include <dma_double_buffer.h>
#include <array>
#include <bitmask_gen.h>

struct DACPins
{
    uint32_t pico;
    uint32_t sck;
    uint32_t cs;
};

using T = uint16_t; // Double Buffer Data Transfer Type.

inline constexpr size_t SHA256_NUM_BYTES = 8;

inline constexpr size_t NUM_CHANNELS = 4;
inline constexpr std::array<const char*, NUM_CHANNELS> filenames
{{
    "channel_0.bin", "channel_1.bin", "channel_2.bin", "channel_3.bin"
}};

inline constexpr size_t WAVEFORM_MAX_WORDS = 5'000'000;
inline constexpr size_t WAVEFORM_MAX_BYTES = WAVEFORM_MAX_WORDS * sizeof(T);

inline constexpr size_t SD_CHUNK_SIZE_BYTES = 32768; // must be a factor of 512
inline constexpr size_t READ_BUF_SIZE = SD_CHUNK_SIZE_BYTES/sizeof(T);

#define DAC_PIO (pio1)
#define SD_PIO (pio0)

// Double Buffers must be accessible by both cores.
extern std::array<PIO_LTC264x, NUM_CHANNELS> dacs;

static_assert(SD_CHUNK_SIZE_BYTES % 512 == 0,
 "SD_CHUNK_SIZE_BYTES must be a multiple of 512 (SD card block size).");


inline constexpr std::array<DACPins, NUM_CHANNELS> DAC_PINS
{{
    {.pico = 4, .sck = 5, .cs = 6},
    {.pico = 8, .sck = 9, .cs = 10},
    {.pico = 12, .sck = 13, .cs = 14},
    {.pico = 16, .sck = 17, .cs = 18}
}};

// SD pins and settings.
inline constexpr size_t SD_CMD_PIN = 22;
inline constexpr size_t SD_D0_PIN = 23;
inline constexpr size_t SD_READ_SPEED_HZ = 150 * 1000 * 1000 / 6; // RP2350: 25 MHz

inline constexpr size_t NUM_DIS = 4;
inline constexpr size_t DI_PORT_BASE = 29;
inline constexpr uint64_t DI_PORT_MASK =
    nwide_mask<uint64_t>(NUM_DIS) << DI_PORT_BASE;

inline constexpr size_t NUM_DOS = 4;
inline constexpr size_t DO_PORT_BASE = 33;
inline constexpr uint64_t DO_PORT_MASK =
    nwide_mask<uint64_t>(NUM_DOS) << DO_PORT_BASE;

inline constexpr size_t NUM_EXT_TRIGGERS = 4;
inline constexpr size_t EXT_TRIGGER_BASE = 29;
inline constexpr uint64_t EXT_TRIGGER_MASK =
    nwide_mask<uint64_t>(NUM_EXT_TRIGGERS) << EXT_TRIGGER_BASE;


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

inline constexpr size_t UNUSED_SERIAL_NUMBER = 0; // Deprecated in favor of R_UUID

#endif // CONFIG_H
