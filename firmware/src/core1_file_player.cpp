#include <core1_file_player.h>

void core1main()
{
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    // Each player only services the channels it currently owns; the other's
    // update() is a cheap no-op for those channels.
    while (true)
    {
        player.update();
        waveform_player.update();
    }
}
