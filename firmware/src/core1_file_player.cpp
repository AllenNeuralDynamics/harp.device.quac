#include <core1_file_player.h>

void core1main()
{
    // Note: Commands like Pause/Resume/Stop are handled by core0.
    while (true)
    {
        uint32_t LED_MASK = (1u << DEBUG_LEDS[0]) | (1u << DEBUG_LEDS[1]);
        gpio_put_masked(LED_MASK, 0xFFFFFFFF); // Toggle LEDs.
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
