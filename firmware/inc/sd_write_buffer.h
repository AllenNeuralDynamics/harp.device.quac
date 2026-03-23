#ifndef SD_WRITE_BUFFER_H
#define SD_WRITE_BUFFER_H

#include <pico/sha256.h>
#include <ff.h>
#include <f_util.h>
#include <cstring>

/**
 * \brief Double-buffered sector writer with concurrent SHA-256 computation.
 *
 * Accumulates data from Harp write messages into 512-byte sectors. Each full
 * sector triggers a flush: a non-blocking DMA transfer to the RP2350 SHA-256
 * accelerator is started first, then an f_write to the SD card runs
 * concurrently (both operations only read the buffer, so there is no
 * conflict). SHA-256 processes at ~79 MB/s and completes in microseconds;
 * f_write takes milliseconds, so the SHA-256 DMA is always done before the
 * next pico_sha256_update/finish call, which is where the implicit DMA-wait
 * occurs. Two equal-sized 512-byte buffers alternate so the idle buffer can
 * be filled from the next Harp payload while the active buffer is in flight.
 *
 * Intended usage (one file at a time):
 * \code
 *   FIL f;
 *   f_open(&f, "channel_0.bin", FA_WRITE | FA_CREATE_ALWAYS);
 *   sd_writer.begin(&f);
 *   sd_writer.write(data_ptr, len);  // call as many times as needed
 *   sha256_result_t hash;
 *   sd_writer.finalize(hash);
 *   f_close(&f);
 * \endcode
 */
class SdWriteBuffer
{
public:
    static constexpr size_t SECTOR_SIZE = 512;

    SdWriteBuffer() : curr_buf_(0), fill_level_(0), file_(nullptr) {}

    /**
     * \brief Begin a new write session.
     * \param file An already-opened FIL handle (FA_WRITE | FA_CREATE_ALWAYS).
     */
    void begin(FIL* file)
    {
        file_ = file;
        curr_buf_ = 0;
        fill_level_ = 0;
        pico_sha256_start_blocking(&sha256_state_, SHA256_BIG_ENDIAN, true);
    }

    /**
     * \brief Append \p len bytes to the write stream.
     *
     * Full 512-byte sectors are immediately flushed: fed to the SHA-256
     * hardware accelerator (via DMA) and written to the SD card.
     *
     * \return false on SD write error; the session should be aborted.
     */
    bool write(const uint8_t* data, size_t len)
    {
        while (len > 0)
        {
            size_t space = SECTOR_SIZE - fill_level_;
            size_t to_copy = (len < space) ? len : space;
            memcpy(&buffers_[curr_buf_][fill_level_], data, to_copy);
            fill_level_ += to_copy;
            data += to_copy;
            len -= to_copy;
            if (fill_level_ == SECTOR_SIZE)
            {
                if (!flush_sector(SECTOR_SIZE))
                    return false;
            }
        }
        return true;
    }

    /**
     * \brief Flush any remaining data, finish the SHA-256 digest, and
     *  invalidate the session.
     * \param[out] result Full 32-byte SHA-256 digest of all data written
     *  since begin().
     * \return false on SD write error.
     */
    bool finalize(sha256_result_t& result)
    {
        if (fill_level_ > 0 && !flush_sector(fill_level_))
            return false;
        pico_sha256_finish(&sha256_state_, &result);
        file_ = nullptr;
        return true;
    }

    bool is_active() const { return file_ != nullptr; }

    /**
     * \brief Abandon the current session without finalizing.
     */
    void abort()
    {
        pico_sha256_cleanup(&sha256_state_);
        file_ = nullptr;
        fill_level_ = 0;
    }

private:
    uint8_t buffers_[2][SECTOR_SIZE];
    size_t curr_buf_;
    size_t fill_level_;
    FIL* file_;
    pico_sha256_state_t sha256_state_;

    /**
     * \brief Feed the active buffer to SHA-256 and write it to the SD card,
     *  then switch to the other buffer.
     * \param bytes Number of valid bytes in the current buffer (≤ SECTOR_SIZE).
     *
     * The SHA-256 DMA and SD-card DMA run concurrently: both are readers of
     * the same buffer so there is no conflict. The buffer must remain
     * unchanged until the next pico_sha256_update / pico_sha256_finish call
     * (API contract), which satisfies the constraint because we do not touch
     * this buffer again until after that next call returns.
     */
    bool flush_sector(size_t bytes)
    {
        // Start SHA-256 DMA (non-blocking) — returns before DMA is complete.
        pico_sha256_update(&sha256_state_, buffers_[curr_buf_], bytes);

        // SD write runs while SHA-256 DMA is in progress. The next
        // pico_sha256_update / pico_sha256_finish call (on the following
        // sector) will wait for this DMA to complete.
        UINT written;
        FRESULT fr = f_write(file_, buffers_[curr_buf_], bytes, &written);
        if (fr != FR_OK || written != static_cast<UINT>(bytes))
            return false;

        curr_buf_ ^= 1;
        fill_level_ = 0;
        return true;
    }
};

#endif // SD_WRITE_BUFFER_H
