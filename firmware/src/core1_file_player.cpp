#include "core1_file_player.h"

void core1main()
{
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    while (true)
    {
        for (auto player_ptr : player_ptrs)
        {
            if (player_ptr->is_busy())
                player_ptr->update();
        }
    }
}
