#ifndef CORE1_FILE_PLAYER_H
#define CORE1_FILE_PLAYER_H
#include <config.h>
#include <waveform_settings.h>
#include "source_player.h"
#include <pico/util/queue.h>
#include <array>

extern std::array<SourcePlayer<T, READ_BUF_SIZE>*, NUM_CHANNELS * NUM_PLAYER_TYPES>
player_ptrs;

void core1main();

#endif // CORE1_FILE_PLAYER_H
