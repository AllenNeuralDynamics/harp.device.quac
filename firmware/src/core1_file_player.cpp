#include <core1_file_player.h>

void core1main()
{
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    while (true)
    {
        for (auto& player: file_players)
        {
            if (player.is_busy())
                player.update();
        }
        for (auto& player: sine_players)
        {
            if (player.is_busy())
                player.update();
        }
        for (auto& player: trapezoid_players)
        {
            if (player.is_busy())
                player.update();
        }
    }
}
