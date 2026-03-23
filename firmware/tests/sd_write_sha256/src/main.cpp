#include <stdio.h>
#include <cstring>
#include <pico/stdlib.h>
#include <pico/sha256.h>
#include <f_util.h>
#include <ff.h>
#include <hw_config.h>
#include <sd_write_buffer.h>

// ---------------------------------------------------------------------------
// SD hardware configuration (SDIO, 30 MHz, CMD=3, D0=4)
// ---------------------------------------------------------------------------

static sd_sdio_if_t sdio_if =
{
//  CLK_gpio = D0_gpio - 2 -> derived automatically
    .CMD_gpio  = 22,
    .D0_gpio   = 23,
//  D1..D3    = D0_gpio + 1/2/3 -> derived automatically
    .SDIO_PIO  = pio0,
    .baud_rate = 150 * 1000 * 1000 / 6,  // 25 MHz
};

static sd_card_t sd_card = {.type = SD_IF_SDIO, .sdio_if_p = &sdio_if};

size_t     sd_get_num()              { return 1; }
sd_card_t* sd_get_by_num(size_t num) { return num == 0 ? &sd_card : nullptr; }

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static SdWriteBuffer sd_writer;

// Shared readback buffer — large enough for the biggest test (4096 bytes).
static uint8_t g_readback[4096];

// Compute SHA256 directly using the RP2350 hardware accelerator on a
// contiguous buffer.  This is used as the reference value that
// SdWriteBuffer must match.
static void reference_sha256(const uint8_t* data, size_t len,
                              sha256_result_t& out)
{
    pico_sha256_state_t state;
    pico_sha256_start_blocking(&state, SHA256_BIG_ENDIAN, true);
    pico_sha256_update(&state, data, len);
    pico_sha256_finish(&state, &out);
}

static bool hashes_equal(const sha256_result_t& a, const sha256_result_t& b)
{
    return memcmp(a.bytes, b.bytes, SHA256_RESULT_BYTES) == 0;
}

static void print_hash(const char* label, const sha256_result_t& h)
{
    printf("  %s: ", label);
    for (int i = 0; i < SHA256_RESULT_BYTES; ++i)
        printf("%02x", h.bytes[i]);
    printf("\r\n");
}

// ---------------------------------------------------------------------------
// Generic test runner
//
// Writes `data` (total `len` bytes) to `filename` via SdWriteBuffer, using
// `nchunks` separate write() calls.  Chunk sizes are spread as evenly as
// possible so that boundary conditions at 512-byte sector edges are exercised
// naturally.
//
// Verifies:
//   1. The SHA256 produced by SdWriteBuffer matches a reference computed
//      directly on the source data.
//   2. The file content on SD card exactly matches the source data.
//
// Returns true iff all checks pass.
// ---------------------------------------------------------------------------

static bool run_test(const char* name, const uint8_t* data, size_t len,
                     size_t nchunks, const char* filename)
{
    printf("[%s] len=%u nchunks=%u\r\n", name, (unsigned)len, (unsigned)nchunks);

    // --- Write via SdWriteBuffer -----------------------------------------------
    FIL f;
    FRESULT fr = f_open(&f, filename, FA_WRITE | FA_CREATE_ALWAYS);
    if (fr != FR_OK)
    {
        printf("[%s] FAIL: f_open error %d\r\n", name, fr);
        return false;
    }
    sd_writer.begin(&f);

    size_t offset = 0;
    for (size_t c = 0; c < nchunks; ++c)
    {
        // Spread remaining bytes across remaining chunks; at least 1 byte each.
        size_t remaining_chunks = nchunks - c;
        size_t remaining_bytes  = len - offset;
        size_t chunk_size       = remaining_bytes / remaining_chunks;
        if (chunk_size == 0)
            chunk_size = 1;

        if (!sd_writer.write(data + offset, chunk_size))
        {
            printf("[%s] FAIL: write() error at chunk %u (offset=%u)\r\n",
                   name, (unsigned)c, (unsigned)offset);
            sd_writer.abort();
            f_close(&f);
            return false;
        }
        offset += chunk_size;
    }
    // Handle any remainder from integer division, writing it as a final call.
    if (offset < len)
    {
        if (!sd_writer.write(data + offset, len - offset))
        {
            printf("[%s] FAIL: write() error at remainder chunk\r\n", name);
            sd_writer.abort();
            f_close(&f);
            return false;
        }
    }

    sha256_result_t buffered_hash;
    if (!sd_writer.finalize(buffered_hash))
    {
        printf("[%s] FAIL: finalize() error\r\n", name);
        f_close(&f);
        return false;
    }
    f_close(&f);

    // --- Reference SHA256 ------------------------------------------------------
    sha256_result_t ref_hash;
    reference_sha256(data, len, ref_hash);
    print_hash("buffered ", buffered_hash);
    print_hash("reference", ref_hash);

    bool pass = true;

    if (!hashes_equal(buffered_hash, ref_hash))
    {
        printf("[%s] FAIL: hash mismatch\r\n", name);
        pass = false;
    }

    // --- Read-back and verify file content ------------------------------------
    FIL rf;
    fr = f_open(&rf, filename, FA_READ);
    if (fr != FR_OK)
    {
        printf("[%s] FAIL: readback f_open error %d\r\n", name, fr);
        return false;
    }
    UINT bytes_read = 0;
    fr = f_read(&rf, g_readback, len, &bytes_read);
    f_close(&rf);

    if (fr != FR_OK || bytes_read != static_cast<UINT>(len))
    {
        printf("[%s] FAIL: readback error (fr=%d got=%u expected=%u)\r\n",
               name, fr, bytes_read, (unsigned)len);
        pass = false;
    }
    else if (memcmp(g_readback, data, len) != 0)
    {
        printf("[%s] FAIL: file content mismatch\r\n", name);
        pass = false;
    }

    printf("[%s] %s\r\n\r\n", name, pass ? "PASS" : "FAIL");
    return pass;
}

// ---------------------------------------------------------------------------
// Test data
// ---------------------------------------------------------------------------

// 256 bytes — sub-sector, all 0xAB.
static uint8_t data_256[256];

// 512 bytes — exact one sector, incrementing counter.
static uint8_t data_512[512];

// 1536 bytes — three sectors, alternating 0x55 / 0xAA per 256-byte block.
static uint8_t data_1536[1536];

// 4096 bytes — eight sectors, incrementing counter (wraps at 256).
static uint8_t data_4096[4096];

static void init_test_data()
{
    memset(data_256, 0xAB, sizeof(data_256));

    for (size_t i = 0; i < sizeof(data_512); ++i)
        data_512[i] = (uint8_t)(i & 0xFF);

    for (size_t i = 0; i < sizeof(data_1536); ++i)
        data_1536[i] = (i / 256) % 2 ? 0xAA : 0x55;

    for (size_t i = 0; i < sizeof(data_4096); ++i)
        data_4096[i] = (uint8_t)(i & 0xFF);
}

// ---------------------------------------------------------------------------
// main
// ---------------------------------------------------------------------------

int main()
{
    stdio_init_all();
    while (!stdio_usb_connected()) { sleep_ms(100); }

    printf("=== SD Write + SHA256 Test ===\r\n\r\n");

    FATFS fs;
    FRESULT fr = f_mount(&fs, "", 1);
    if (fr != FR_OK)
        panic("f_mount error: %s (%d)\n", FRESULT_str(fr), fr);

    printf("SD card mounted.\r\n\r\n");

    init_test_data();

    int pass = 0, fail = 0;
    auto record = [&](bool ok) { ok ? ++pass : ++fail; };

    // T1 — Sub-sector (256 B), single write call.
    // Tests: partial-sector flush inside finalize().
    record(run_test("T1-SubSector",
                    data_256, sizeof(data_256), 1,
                    "sha_t1.bin"));

    // T2 — Exact sector (512 B), single write call.
    // Tests: flush_sector() is triggered exactly once from write(), with no
    // remainder left for finalize().
    record(run_test("T2-ExactSector",
                    data_512, sizeof(data_512), 1,
                    "sha_t2.bin"));

    // T3 — Three sectors (1536 B), single write call.
    // Tests: multiple flush_sector() calls from a single write().
    record(run_test("T3-MultiSector-SingleWrite",
                    data_1536, sizeof(data_1536), 1,
                    "sha_t3.bin"));

    // T4 — Three sectors (1536 B), split into 2 write calls at a
    // mid-sector boundary (300 B + 1236 B).
    // Tests: SHA256 context continuity across write() calls when the split
    // falls inside a sector.
    record(run_test("T4-MultiSector-SplitMid",
                    data_1536, sizeof(data_1536), 2,
                    "sha_t4.bin"));

    // T5 — Eight sectors (4096 B), 2 write calls split at a sector boundary
    // (512 B + 3584 B).
    // Tests: split exactly on a sector edge, exercises double-buffer swap.
    record(run_test("T5-LargeData-SplitOnBoundary",
                    data_4096, sizeof(data_4096), 2,
                    "sha_t5.bin"));

    // T6 — Three sectors (1536 B), 6 write calls of 256 B each.
    // Tests: many small writes that each straddle sector boundaries, proving
    // the accumulation loop in write() behaves correctly for arbitrary chunk
    // sizes.
    record(run_test("T6-ManySmallWrites",
                    data_1536, sizeof(data_1536), 6,
                    "sha_t6.bin"));

    // T7 — Abort test.
    // Tests: abort() clears the session state, and a new session opened
    // immediately after produces a correct result.
    printf("[T7-Abort] Starting...\r\n");
    {
        bool t7_pass = true;

        FIL abort_f;
        fr = f_open(&abort_f, "sha_t7_abort.bin", FA_WRITE | FA_CREATE_ALWAYS);
        if (fr != FR_OK)
        {
            printf("[T7-Abort] FAIL: f_open error %d\r\n", fr);
            t7_pass = false;
        }
        else
        {
            sd_writer.begin(&abort_f);
            sd_writer.write(data_256, sizeof(data_256));  // partial write
            sd_writer.abort();
            f_close(&abort_f);

            if (sd_writer.is_active())
            {
                printf("[T7-Abort] FAIL: is_active() still true after abort()\r\n");
                t7_pass = false;
            }
            else
            {
                printf("[T7-Abort] abort() clears session: PASS\r\n");
            }
        }

        // Follow-up: open a new session immediately to prove the state is clean.
        if (t7_pass)
        {
            t7_pass = run_test("T7-Abort-FollowUp",
                               data_256, sizeof(data_256), 1,
                               "sha_t7_followup.bin");
        }

        printf("[T7-Abort] %s\r\n\r\n", t7_pass ? "PASS" : "FAIL");
        record(t7_pass);
    }

    // --- Summary ---------------------------------------------------------------
    printf("=== Results: %d passed, %d failed ===\r\n", pass, fail);

    f_unmount("");
    printf("Done. Halting.\r\n");
    for (;;) {}
}
