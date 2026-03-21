#include <core1_file_player.h>

void setup_file_player()
{
    //MultiFilePlayer player(dacs, filenames);
}


int core1main()
{
    WaveformSettings settings;
    BulkWaveformStates last_bulk_state;

    setup_file_player();
    BulkWaveformStates last_bulk_state = player.get_bulk_state();
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    while (true)
    {
        // Handle user waveform settings input from core0.
        //Settings settings;
        while (queue_try_remove(&waveform_settings_queue, &bulk_settings)
        {
            // apply new settings.
            // TODO: Can you apply these while the waveform is playing??
        }
        // update file player loop: top off buffers, add/remove files.
        update();
        // TODO: If waveform finished, push finish time to core0.
        //  Or consider attaching the interrupt on core0??

        //// If the state changes between updates, push it back to core0;
        //BulkWaveformStates curr_bulk_state = player.get_bulk_state();
        //if (curr_bulk_state != last_bulk_state)
        //{
        //    queue_try_add(&bulk_waveform_states_queue, curr_bulk_state);
        //    last_bulk_state = curr_bulk_state;
        //}
    }
}
