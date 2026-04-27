#include <core1_file_player.h>

void core1main()
{
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    while (true)
    {
        for (auto& file_player: file_players)
            file_player.update();
    }
}
