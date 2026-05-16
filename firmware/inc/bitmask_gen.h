#ifndef BITMASK_GEN_H
#define BITMASK_GEN_H
#include <cstdint>
#include <limits>

/**
 * \brief return bitmask at compile-time where \p n specifies the number of
 * consecutive bits.
 */
template <typename T>
consteval T nwide_mask(size_t n)
{
    if (n >= sizeof(T) * 8)
        return std::numeric_limits<T>::max();
    return T{(1ull << n) - 1ull};
}

#endif // BITMASK_GEN_H
