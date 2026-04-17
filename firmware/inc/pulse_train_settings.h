#ifndef PULSE_TRAIN_SETTINGS_H
#define PULSE_TRAIN_SETTINGS_H
#include <cstdint>

/**
 * \brief settings for the pulse train generator (trapezoidal pulses).
 *
 * One period has four regions:
 *   1) linear ramp 0 -> amplitude over ramp_on_duration_us
 *   2) flat at amplitude for pulse_width_us
 *   3) linear ramp amplitude -> 0 over ramp_off_duration_us
 *   4) quiet at 0 for the remainder of pulse_interval_us
 * The output is baselined at DAC midscale (0V on the bipolar +/-10V board),
 * with a peak of `midscale + pulse_amplitude`.
 */
#pragma pack(push, 1)
struct PulseTrainSettings
{
    uint32_t pulse_width_us;         // flat-top duration per pulse.
    uint32_t pulse_interval_us;      // pulse-to-pulse period.
    uint16_t pulse_amplitude;        // peak offset above midscale in DAC codes.
    uint32_t ramp_on_duration_us;    // leading-edge ramp time.
    uint32_t ramp_off_duration_us;   // trailing-edge ramp time.
    uint32_t total_duration_us;      // 0 = run forever.
    uint8_t  external_trigger_mask;  // DI pin(s) that arm this channel.
};
#pragma pack(pop)

#endif // PULSE_TRAIN_SETTINGS_H
