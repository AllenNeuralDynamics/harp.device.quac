#ifndef MULTI_WAVEFORM_PLAYER_H
#define MULTI_WAVEFORM_PLAYER_H

#include <array>
#include <cstdint>
#include <limits>
#include <hardware/dma.h>
#include <dma_double_buffer.h>
#include <multi_file_player.h>
#include <sine_wave_settings.h>
#include <pulse_train_settings.h>
#include <raised_cosine_lut.h>

/**
 * \brief selects the waveform shape for a channel in the waveform player.
 */
enum class WaveformType : uint8_t
{
    Sine = 0,
    PulseTrain = 1,
};

/**
 * \brief generates raised-cosine sinusoids and trapezoidal pulse trains and
 *  streams them to the DACs via the MultiFilePlayer's DMA transport.
 *
 * The waveform player does not own any DMA channels of its own; it borrows
 * the ping-pong DMA buffers from the file player so that the two sources
 * can coexist without exceeding the RP2350 DMA channel budget. At any moment
 * a channel is owned by at most one source:
 *   * `file_player.abandon_files(mask)` relinquishes a set of channels so the
 *     waveform player can drive them.
 *   * `file_player.reclaim_files(mask)` gives them back (reopens files,
 *     re-primes buffers).
 *
 * Sample output is baselined at DAC midscale (0 V on the bipolar +/-10 V
 * board). Raised-cosine sine output swings from midscale up to
 * `midscale + amplitude`; trapezoidal pulses share the same convention for
 * their flat-top value.
 */
template <typename T, size_t NUM_CHANNELS, size_t BUF_SIZE>
class MultiWaveformPlayer
{
public:
    static inline constexpr T OUTPUT_MIDSCALE = std::numeric_limits<T>::max() / 2;
    static inline constexpr T OUTPUT_MAX      = std::numeric_limits<T>::max();
    static inline constexpr uint32_t DEFAULT_SAMPLE_RATE_HZ = 10'000;

    explicit MultiWaveformPlayer(MultiFilePlayer<T, NUM_CHANNELS, BUF_SIZE>& file_player)
    : file_player_{file_player}, sample_rate_hz_{DEFAULT_SAMPLE_RATE_HZ}
    {
        reset();
    }

/**
 * \brief clear all per-channel state. Does not touch the underlying DMA
 *  transport (which lives on the file player).
 * \warning not multicore safe.
 */
    void reset()
    {
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
        {
            active_[i]          = false;
            idle_buffers_[i]    = nullptr;
            phase_q32_[i]       = 0;
            phase_inc_q32_[i]   = 0;
            samples_emitted_[i] = 0;
            samples_total_[i]   = 0;
            period_counter_[i]  = 0;
            interval_samples_[i]= 0;
            width_samples_[i]   = 0;
            ramp_on_samples_[i] = 0;
            ramp_off_samples_[i]= 0;
            waveform_type_[i]   = WaveformType::Sine;
            sine_settings_[i]   = SineWaveSettings{};
            pulse_settings_[i]  = PulseTrainSettings{};
        }
    }

/**
 * \brief cache the sample rate used to convert duration/interval parameters
 *  from microseconds to samples at `start()` time.
 * \note the actual DMA pacing timer lives on the file player; callers must
 *  update both sides via `file_player.set_frequency_hz(hz)` at the same time.
 */
    void set_sample_rate_hz(uint32_t hz)
    {sample_rate_hz_ = hz == 0 ? DEFAULT_SAMPLE_RATE_HZ : hz;}

    uint32_t get_sample_rate_hz() const
    {return sample_rate_hz_;}

/**
 * \brief latch the per-channel waveform parameters. Takes effect on the
 *  next `start()`.
 */
    void set_sine_settings(size_t ch, const SineWaveSettings& s)
    {sine_settings_[ch] = s;}

    void set_pulse_settings(size_t ch, const PulseTrainSettings& s)
    {pulse_settings_[ch] = s;}

    void set_waveform_type(size_t ch, WaveformType t)
    {waveform_type_[ch] = t;}

    WaveformType get_waveform_type(size_t ch) const
    {return waveform_type_[ch];}

    const SineWaveSettings& get_sine_settings(size_t ch) const
    {return sine_settings_[ch];}

    const PulseTrainSettings& get_pulse_settings(size_t ch) const
    {return pulse_settings_[ch];}

/**
 * \brief begin waveform playback on the specified channels.
 * \details for each channel in \p mask:
 *   * channel is taken from the file player via `abandon_files`.
 *   * DMA config is reset and generator state is zeroed.
 *   * the first ping-pong buffer is pre-filled via `update()`.
 *   * the DMA chain is triggered.
 * All channels in \p mask start simultaneously (single DMA trigger mask write).
 * \note can be called from either core.
 */
    void start(uint32_t mask)
    {
        if (mask == 0)
            return;
        // Relinquish channels from the file player so its update() stops
        // touching them.
        file_player_.abandon_files(mask);
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!(mask & (1u << ch)))
                continue;
            auto& buf = file_player_.get_file_buf(ch);
            buf.abort_transfer();
            buf.reset_transfer_config();

            // Reset generator state.
            phase_q32_[ch]       = 0;
            samples_emitted_[ch] = 0;
            period_counter_[ch]  = 0;
            idle_buffers_[ch]    = nullptr;

            const uint64_t sr = sample_rate_hz_;
            if (waveform_type_[ch] == WaveformType::Sine)
            {
                const SineWaveSettings& s = sine_settings_[ch];
                // phase_inc = (freq * 2^32) / sample_rate.
                phase_inc_q32_[ch] = static_cast<uint32_t>(
                    (static_cast<uint64_t>(s.frequency_hz) << 32) / sr);
                samples_total_[ch] = (s.duration_us == 0)
                    ? 0u
                    : static_cast<uint32_t>(
                          (static_cast<uint64_t>(s.duration_us) * sr) / 1'000'000ull);
            }
            else // PulseTrain
            {
                const PulseTrainSettings& p = pulse_settings_[ch];
                interval_samples_[ch] = static_cast<uint32_t>(
                    (static_cast<uint64_t>(p.pulse_interval_us) * sr) / 1'000'000ull);
                if (interval_samples_[ch] == 0)
                    interval_samples_[ch] = 1; // avoid modulo-by-zero.
                width_samples_[ch] = static_cast<uint32_t>(
                    (static_cast<uint64_t>(p.pulse_width_us) * sr) / 1'000'000ull);
                ramp_on_samples_[ch] = static_cast<uint32_t>(
                    (static_cast<uint64_t>(p.ramp_on_duration_us) * sr) / 1'000'000ull);
                ramp_off_samples_[ch] = static_cast<uint32_t>(
                    (static_cast<uint64_t>(p.ramp_off_duration_us) * sr) / 1'000'000ull);
                samples_total_[ch] = (p.total_duration_us == 0)
                    ? 0u
                    : static_cast<uint32_t>(
                          (static_cast<uint64_t>(p.total_duration_us) * sr) / 1'000'000ull);
            }
            active_[ch] = true;
        }
        // Pre-fill the first ping-pong buffer for each requested channel.
        update();
        // Trigger all selected channels simultaneously via a single DMA
        // control-channel trigger-mask write.
        uint32_t trigger_mask = 0;
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!(mask & (1u << ch)))
                continue;
            trigger_mask |=
                (1u << file_player_.get_file_buf(ch).get_ctrl_channel());
        }
        if (trigger_mask != 0)
            dma_start_channel_mask(trigger_mask);
    }

/**
 * \brief abort any in-flight waveform on the specified channels.
 * \details the shared end-of-transfer ISR will still fire for the aborted
 *  channels, which will push a finished-event that the app loop dispatches
 *  as a waveform_finished Harp event.
 */
    void abort(uint32_t mask)
    {
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!(mask & (1u << ch)) || !active_[ch])
                continue;
            file_player_.get_file_buf(ch).abort_transfer();
        }
    }

/**
 * \brief called by the app loop when a finished-transfer event was emitted
 *  for channels owned by this player. Clears active state.
 */
    void note_finished(uint32_t mask)
    {
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!(mask & (1u << ch)))
                continue;
            active_[ch] = false;
        }
    }

/**
 * \brief true if this player currently owns the specified channel.
 */
    inline bool channel_is_busy(size_t ch) const
    {return active_[ch];}

/**
 * \brief true if the specified channel is currently armed and ready to start.
 *  Used by the external-trigger handler.
 */
    inline bool channel_is_ready(size_t ch) const
    {return !active_[ch];}

/**
 * \brief bitmask of channels currently owned by this player.
 */
    uint32_t active_channels_mask() const
    {
        uint32_t m = 0;
        for (size_t i = 0; i < NUM_CHANNELS; ++i)
            if (active_[i]) m |= (1u << i);
        return m;
    }

/**
 * \brief return the external-trigger DI mask configured for the given
 *  channel's currently-selected waveform type.
 */
    uint8_t get_external_trigger_mask(size_t ch) const
    {
        return waveform_type_[ch] == WaveformType::Sine
            ? sine_settings_[ch].external_trigger_mask
            : pulse_settings_[ch].external_trigger_mask;
    }

/**
 * \brief core1 update: refill the idle ping-pong buffer for each active
 *  channel with freshly-generated samples, and terminate the DMA chain when
 *  a bounded-duration waveform has emitted all of its samples.
 */
    void update()
    {
        for (size_t ch = 0; ch < NUM_CHANNELS; ++ch)
        {
            if (!active_[ch])
                continue;
            auto& buf = file_player_.get_file_buf(ch);

            // If the last transfer was scheduled, wait for it to complete;
            // its end-of-transfer IRQ will fire and the app loop will call
            // note_finished() to clear active_[ch].
            if (buf.dma_chain_loop_disconnected() && buf.is_transferring())
                continue;

            T* curr_idle = buf.get_idle_buffer();
            if (curr_idle == nullptr)
                continue;
            if (idle_buffers_[ch] == curr_idle)
                continue; // buffer hasn't switched yet.
            idle_buffers_[ch] = curr_idle;

            // Determine how many samples to emit into this buffer.
            size_t to_generate = BUF_SIZE;
            bool is_last = false;
            if (samples_total_[ch] != 0)
            {
                uint32_t remaining = samples_total_[ch] - samples_emitted_[ch];
                if (remaining <= BUF_SIZE)
                {
                    to_generate = remaining;
                    is_last = true;
                }
            }
            generate_chunk(ch, curr_idle, to_generate);
            // Pad the trailing portion of a short final buffer with midscale
            // so the DAC output sits at 0 V until the DMA terminates.
            for (size_t i = to_generate; i < BUF_SIZE; ++i)
                curr_idle[i] = OUTPUT_MIDSCALE;
            samples_emitted_[ch] += static_cast<uint32_t>(to_generate);

            if (is_last)
            {
                buf.setup_last_dma_transfer(to_generate);
                // Clear idle tracking so we don't try to refill the (now
                // doomed) buffer on subsequent update() calls before the
                // end-of-transfer IRQ fires.
                idle_buffers_[ch] = nullptr;
            }
        }
    }

private:
    void generate_chunk(size_t ch, T* dst, size_t n)
    {
        if (waveform_type_[ch] == WaveformType::Sine)
            generate_sine(ch, dst, n);
        else
            generate_pulse(ch, dst, n);
    }

    static inline T saturating_offset(uint32_t offset)
    {
        uint32_t sample = static_cast<uint32_t>(OUTPUT_MIDSCALE) + offset;
        if (sample > OUTPUT_MAX)
            sample = OUTPUT_MAX;
        return static_cast<T>(sample);
    }

    void generate_sine(size_t ch, T* dst, size_t n)
    {
        uint32_t phase = phase_q32_[ch];
        const uint32_t inc = phase_inc_q32_[ch];
        const uint32_t amp = sine_settings_[ch].amplitude;
        for (size_t i = 0; i < n; ++i)
        {
            const uint32_t idx  = phase >> 22;            // top 10 bits
            const uint32_t next = (idx + 1) & RAISED_COSINE_LUT_MASK;
            const uint32_t frac = phase & 0x003FFFFFu;    // low 22 bits
            const int64_t a = static_cast<int64_t>(RAISED_COSINE_LUT[idx]);
            const int64_t b = static_cast<int64_t>(RAISED_COSINE_LUT[next]);
            // Linear interpolation in [0, 65535].
            const int64_t interp =
                a + ((b - a) * static_cast<int64_t>(frac)) / (int64_t(1) << 22);
            const uint32_t shape = static_cast<uint32_t>(
                interp < 0 ? 0 : (interp > 65535 ? 65535 : interp));
            const uint32_t offset = (shape * amp) >> 16;
            dst[i] = saturating_offset(offset);
            phase += inc;
        }
        phase_q32_[ch] = phase;
    }

    void generate_pulse(size_t ch, T* dst, size_t n)
    {
        uint32_t t              = period_counter_[ch];
        const uint32_t interval = interval_samples_[ch];
        const uint32_t width    = width_samples_[ch];
        const uint32_t ramp_on  = ramp_on_samples_[ch];
        const uint32_t ramp_off = ramp_off_samples_[ch];
        const uint32_t amp      = pulse_settings_[ch].pulse_amplitude;

        const uint32_t plateau_end  = ramp_on + width;
        const uint32_t ramp_down_end = plateau_end + ramp_off;

        for (size_t i = 0; i < n; ++i)
        {
            uint32_t level = 0;
            if (t < ramp_on)
            {
                level = (ramp_on == 0)
                    ? amp
                    : static_cast<uint32_t>(
                        (static_cast<uint64_t>(amp) * t) / ramp_on);
            }
            else if (t < plateau_end)
            {
                level = amp;
            }
            else if (t < ramp_down_end)
            {
                const uint32_t dt = t - plateau_end;
                level = (ramp_off == 0)
                    ? 0
                    : static_cast<uint32_t>(
                        static_cast<uint64_t>(amp) -
                        (static_cast<uint64_t>(amp) * dt) / ramp_off);
            }
            else
            {
                level = 0;
            }
            dst[i] = saturating_offset(level);
            if (++t >= interval)
                t = 0;
        }
        period_counter_[ch] = t;
    }

    MultiFilePlayer<T, NUM_CHANNELS, BUF_SIZE>& file_player_;
    uint32_t sample_rate_hz_;

    // Per-channel state.
    std::array<bool, NUM_CHANNELS> active_{};
    std::array<WaveformType, NUM_CHANNELS> waveform_type_{};
    std::array<SineWaveSettings, NUM_CHANNELS> sine_settings_{};
    std::array<PulseTrainSettings, NUM_CHANNELS> pulse_settings_{};
    std::array<T*, NUM_CHANNELS> idle_buffers_{};

    // Sine DDS state.
    std::array<uint32_t, NUM_CHANNELS> phase_q32_{};
    std::array<uint32_t, NUM_CHANNELS> phase_inc_q32_{};

    // Pulse train state.
    std::array<uint32_t, NUM_CHANNELS> period_counter_{};
    std::array<uint32_t, NUM_CHANNELS> interval_samples_{};
    std::array<uint32_t, NUM_CHANNELS> width_samples_{};
    std::array<uint32_t, NUM_CHANNELS> ramp_on_samples_{};
    std::array<uint32_t, NUM_CHANNELS> ramp_off_samples_{};

    // Shared duration tracking.
    std::array<uint32_t, NUM_CHANNELS> samples_emitted_{};
    std::array<uint32_t, NUM_CHANNELS> samples_total_{}; // 0 = forever.
};

#endif // MULTI_WAVEFORM_PLAYER_H
