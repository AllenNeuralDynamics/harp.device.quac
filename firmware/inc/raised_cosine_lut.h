#ifndef RAISED_COSINE_LUT_H
#define RAISED_COSINE_LUT_H
#include <array>
#include <cstddef>
#include <cstdint>

/**
 * \file raised_cosine_lut.h
 * \brief compile-time raised-cosine lookup table for the sine generator.
 *
 * The LUT stores (1 - cos(2*pi*i/N))/2 in the range [0, 65535] for
 * i = 0 .. N-1. Using 10 MSBs of a Q32 phase accumulator plus the 22 LSBs
 * as a linear-interpolation weight gives ~20-bit effective resolution, which
 * is enough for sub-Hz sinusoids at a 10 kHz sample rate (10k samples/cycle).
 */

inline constexpr size_t RAISED_COSINE_LUT_SIZE = 1024;
inline constexpr size_t RAISED_COSINE_LUT_MASK = RAISED_COSINE_LUT_SIZE - 1;

namespace quac_detail
{
/// Compile-time cosine via truncated Taylor series. Good to ~1e-12 over
/// the argument range after modular reduction.
consteval double cexpr_cos(double x)
{
    constexpr double pi = 3.14159265358979323846;
    constexpr double two_pi = 2.0 * pi;
    // Reduce x to (-pi, pi].
    while (x > pi)      x -= two_pi;
    while (x <= -pi)    x += two_pi;
    double x2 = x * x;
    double term = 1.0;
    double result = 1.0;
    for (int n = 1; n < 18; ++n)
    {
        term = -term * x2 / double((2 * n - 1) * (2 * n));
        result += term;
    }
    return result;
}

consteval std::array<uint16_t, RAISED_COSINE_LUT_SIZE> make_raised_cosine_lut()
{
    constexpr double pi = 3.14159265358979323846;
    std::array<uint16_t, RAISED_COSINE_LUT_SIZE> lut{};
    for (size_t i = 0; i < RAISED_COSINE_LUT_SIZE; ++i)
    {
        double phase = 2.0 * pi * double(i) / double(RAISED_COSINE_LUT_SIZE);
        double normalized = (1.0 - cexpr_cos(phase)) / 2.0; // [0, 1]
        double scaled = normalized * 65535.0 + 0.5;
        if (scaled > 65535.0) scaled = 65535.0;
        if (scaled < 0.0)     scaled = 0.0;
        lut[i] = static_cast<uint16_t>(scaled);
    }
    return lut;
}
} // namespace quac_detail

inline constexpr auto RAISED_COSINE_LUT = quac_detail::make_raised_cosine_lut();

#endif // RAISED_COSINE_LUT_H
