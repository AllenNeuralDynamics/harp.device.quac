#ifndef FILE_PLAYER_H
#define FILE_PLAYER_H
#include <cstring>
#include "ff.h"
#include "source_player.h"
#include "waveform_settings.h"

/**
 * \brief class for reading a file into a buffer
 */
template <typename T, size_t BUF_SIZE>
class FilePlayer: public SourcePlayer<T, BUF_SIZE>
{
public:
    FilePlayer()
    : SourcePlayer<T, BUF_SIZE>{}, filptr_{nullptr}, settings_{}
    {
        this->settings_ptr_ = &settings_;
    }

/**
 * \brief Put the FilePlayer in a state where it is ready to be triggered
 * immediately.
 */
    void setup() override
    {
        open_file(settings_.path);
        SourcePlayer<T, BUF_SIZE>::setup();
    }

/**
 * \brief
 */
    bool apply_settings(FileSettings& settings)
    {
        /// FIXME: use FileSettings, not WaveformSettings to get file name.
        if ((this->buf_ptr_ == nullptr) || this->is_busy())
            return false;
        settings_ = settings; // copy settings so rewind_source() works.
        return apply_settings((WaveformSettings&)settings); // upcast.
    }

/**
 * \brief apply base waveform settings. Update current FileSettings to match.
 */
    bool apply_settings(WaveformSettings& settings) override
    {
        // Copy all WaveformSettings-related settings from
        // WaveformSettings parameter into our FileSettings member;
        static_cast<WaveformSettings&>(settings_) = settings;
        // Call parent to trigger the underlying reset().
        if (!SourcePlayer<T, BUF_SIZE>::apply_settings(settings)) // will reset()
            return false;
        // if the file is open, reopen it to refresh the update loop with
        // the new settings.
        if (file_is_open())
            return open_file(settings_.path);
        return true;
    }

/**
* \brief return a read-only reference to the current settings
*/
    const WaveformSettings& get_settings() const
    {return settings_;}

/**
 *  \brief open the previously specified file or a new one with the current
 *  settings. Arm the buffer (if specified).
 *  Idempotent.
 */
    inline bool open_file(const char* filepath = nullptr)
    {
        if (!filepath)
            filepath = settings_.path;
        if (file_is_open())
        {close_file();}
        strcpy(settings_.path, filepath); // update settings_.
        // f_open will fail if FileSettings were never specified and
        // filename is not passed in or filename is not found on SD card.
        if (f_open(&fil_, filepath, FA_READ) != FR_OK)
        {return false;}
        filptr_ = &fil_;
        return true;
    }

/**
*  \brief close any previously opened file.
*/
    inline void close_file()
    {
        if (filptr_ != nullptr)
            f_close(&fil_);
        filptr_ = nullptr; // clear ptr to indicate closed file.
        this->reset(); // release armed or busy state conditions.
        // Do not alter \ref curr_filename_.
    }

/**
 * \brief true if this class currently has any file open.
 */
    inline bool file_is_open()
    {return filptr_ != nullptr;}

/**
 * \brief release claimed resources.
 */
    void cleanup() override
    {
        SourcePlayer<T, BUF_SIZE>::cleanup();
        if (file_is_open())
            close_file();
    }

protected:
/**
 * \brief rewind file so it's ready to be played again from the beginning.
 */
    inline void rewind_source()
    {
        if (file_is_open())
            f_rewind(&fil_);
    }

/**
 * \brief transfer bytes from file to the address specified in \p dest.
 */
    inline void transfer_source_chunk(T* dest, size_t num_bytes,
                               size_t& bytes_transferred)
    {
        FRESULT fr = f_read(&fil_, dest, this->CHUNK_SIZE_BYTES,
                            &bytes_transferred);
        if (fr != FR_OK) // TODO: better error handling instead of panicking.
        {panic("Could not read data from file!\r\n");}
    }

/**
 * \brief true if the file has been fully read to the end.
 */
    inline bool source_finished()
    {return f_eof(&fil_);}

private:
    FIL fil_;
    FIL* filptr_; // pointer to fil_ to track if file is open.
    FileSettings settings_;
};
#endif // FILE_PLAYER_H
