#ifndef FILE_PLAYER_H
#define FILE_PLAYER_H
#include "source_player.h"
#include "ff.h"
#include "f_util.h"
#include "waveform_settings.h"
#include <cstring>

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
 * \brief
 */
    bool apply_settings(WaveformSettings& settings) override
    {
        if ((this->buf_ptr_ == nullptr) || this->is_busy())
            return false;
        settings_ = settings; // copy settings so rewind_source() works.
        return SourcePlayer<T, BUF_SIZE>::apply_settings(settings);
        // if the file is open, reopen it to refresh the update loop with
        // the new settings.
        if (file_is_open())
            open_file(curr_filename_);
    }

/**
* \brief return a read-only reference to the current settings
*/
    const WaveformSettings& get_settings() const
    {return settings_;}

/**
 *  \brief open the previously specified file.
 *  \details idempotent.
 */
    inline void open_file(const char* filename)
    {
        if (file_is_open())
        {close_file();}
        if (f_open(&fil_, filename, FA_READ) != FR_OK)
        {panic("Could not open: %s.\r\n", filename);}
        filptr_ = &fil_;
        strcpy(curr_filename_, filename);
        // pre-read buffers (if buffer is claimed).
        SourcePlayer<T, BUF_SIZE>::update();
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
    WaveformSettings settings_;
    char curr_filename_[64];
};
#endif // FILE_PLAYER_H
