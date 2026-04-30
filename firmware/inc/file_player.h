#ifndef FILE_PLAYER_H
#define FILE_PLAYER_H
#include "source_player.h"
#include "ff.h"
#include "f_util.h"


// FIXME: implement read-subset-of-file: i.e: duration_us specified.

/**
 * \brief class for reading a file into a buffer
 */
template <typename T, size_t BUF_SIZE>
class FilePlayer: public SourcePlayer<T, BUF_SIZE>
{
public:
    FilePlayer(DMADoubleBuffer<T, BUF_SIZE>* buf_ptr = nullptr)
    : SourcePlayer<T, BUF_SIZE>{buf_ptr}, filptr_{nullptr}
    {}

/**
 *  \brief open the previously specified file.
 *  \details idempotent.
 */
    inline void open_file(const char* filename)
    {
        if (file_is_open())
        {close_file();}
        if (f_open(filptr_, filename, FA_READ) != FR_OK)
        {panic("Could not open: %s\r\n", filename);}
        // pre-read buffers (if buffer is claimed).
        SourcePlayer<T, BUF_SIZE>::update();
    }

/**
*  \brief close any previously opened file.
*/
    inline void close_file()
    {
        if (filptr_ != nullptr)
            f_close(filptr_);
        filptr_ = nullptr; // clear ptr to indicate closed file.
        this->idle_buf_ptr_ = nullptr; // Clear local buffer value.
        this->curr_cycles_ = 0;
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

/**
 * \brief rewind file so it's ready to be played again from the beginning.
 */
    inline void rewind_source()
    {f_rewind(filptr_);}

/**
 * \brief transfer bytes from file to the address specified in \p dest.
 */
    inline void transfer_source_chunk(T* dest, size_t num_bytes,
                               size_t& bytes_transferred)
    {
        FRESULT fr = f_read(filptr_, dest, this->CHUNK_SIZE_BYTES,
                            &bytes_transferred);
        if (fr != FR_OK) // TODO: better error handling instead of panicking.
        {panic("Could not read data from file!\r\n");}
    }

/**
 * \brief true if the file has been fully read to the end.
 */
    inline bool source_finished()
    {return f_eof(filptr_);}

private:
    FIL* filptr_;
};
#endif // FILE_PLAYER_H
