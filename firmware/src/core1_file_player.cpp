#include <core1_file_player.h>

void core1main()
{
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    while (true)
        player.update();
}
