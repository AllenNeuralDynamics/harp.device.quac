#ifndef FUNCTION_PLAYER_H
#define FUNCTION_PLAYER_H
#include <cstdint>
#include <limits>
#include "source_player.h"

/**
 * \brief base class for creating waveforms from time-series functions and
 *  streaming them to the specified buffer.
 */
template <typename T, size_t BUF_SIZE>
class FunctionPlayer: public SourcePlayer<T, BUF_SIZE>
{
public:
    static inline constexpr T OUTPUT_MIDSCALE = std::numeric_limits<T>::max() / 2;
    static inline constexpr T OUTPUT_MAX      = std::numeric_limits<T>::max();
    //static inline constexpr uint32_t DEFAULT_SAMPLE_RATE_HZ = 10'000;

    FunctionPlayer()
    : SourcePlayer<T, BUF_SIZE>{}{}


    void setup()
    {
        // pre-read buffers (if buffer is claimed).
        SourcePlayer<T, BUF_SIZE>::update();
    }

protected:
/**
 * \brief true if the file has been fully read to the end.
 */
    inline virtual bool source_finished()
    {
        if (this->settings_ptr_->duration_us == 0) // never finished in this case.
            return false;
        return this->sample_count_ == this->samples_emitted_;
    }

/**
 * \brief
 */
    virtual void generate_function_chunk(T* dest, size_t num_samples) = 0;

/**
 * \brief transfer bytes from file to the address specified in \p dest.
 */
    inline void transfer_source_chunk(T* dest, size_t num_bytes,
                                      size_t& bytes_transferred)
    {
        uint32_t num_samples = num_bytes/sizeof(T);
        generate_function_chunk(dest, num_samples);
        bytes_transferred = sizeof(T) * num_samples;
    }

/**
 * \brief
 */
    static inline T saturating_offset(uint32_t offset)
    {
        //uint32_t sample = static_cast<uint32_t>(OUTPUT_MIDSCALE) + offset;
        uint32_t sample = offset;
        if (sample > OUTPUT_MAX)
            sample = OUTPUT_MAX;
        return static_cast<T>(sample);
    }

};
#endif // FUNCTION_PLAYER_H
