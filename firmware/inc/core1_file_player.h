#ifndef CORE1_FILE_PLAYER_H
#define CORE1_FILE_PLAYER_H
#include <config.h>
#include <waveform_settings.h>
#include "file_player.h"
#include <pico/util/queue.h>

//extern queue_t waveform_settings_queue;
//extern queue_t bulk_waveform_states_queue;

extern std::array<FilePlayer<T, READ_BUF_SIZE>, NUM_CHANNELS> file_players;

void core1main();

#endif // CORE1_FILE_PLAYER_H
