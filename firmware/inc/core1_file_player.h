#ifndef CORE1_FILE_PLAYER_H
#define CORE1_FILE_PLAYER_H
#include <config.h>
#include <waveform_settings.h>
#include <multi_file_player.h>
#include <multi_waveform_player.h>
#include <pico/util/queue.h>

//extern queue_t waveform_settings_queue;
//extern queue_t bulk_waveform_states_queue;

extern MultiFilePlayer<T, NUM_CHANNELS, READ_BUF_SIZE> player;
extern MultiWaveformPlayer<T, NUM_CHANNELS, READ_BUF_SIZE> waveform_player;

void core1main();

#endif // CORE1_FILE_PLAYER_H
