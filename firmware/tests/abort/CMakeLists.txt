cmake_minimum_required(VERSION 3.13)
find_package(Git REQUIRED)
execute_process(COMMAND "${GIT_EXECUTABLE}" rev-parse --short HEAD OUTPUT_VARIABLE COMMIT_ID OUTPUT_STRIP_TRAILING_WHITESPACE)
message(STATUS "Computed Git Hash: ${COMMIT_ID}")
add_definitions(-DGIT_HASH="${COMMIT_ID}") # Usable in source code.

#add_definitions(-DDEBUG) # Uncomment for debugging

set(PICO_BOARD pico2)
set(PICO_PLATFORM rp2350-arm-s)

if (DEFINED PICO_SDK_PATH)
    message("Using explicitly defined PICO_SDK_PATH.")
elseif (DEFINED ENV{PICO_SDK_PATH} AND NOT DEFINED PICO_SDK_PATH)
    set(PICO_SDK_PATH "$ENV{PICO_SDK_PATH}")
    add_definitions(-DPICO_SDK_PATH="$ENV{PICO_SDK_PATH}")
    message("Using environment variable PICO_SDK_PATH.")
else()
    message(SEND_ERROR "PICO_SDK_PATH is not defined either as an environment "
"variable or explicitly via 'cmake -DPICO_SDK_PATH=<path to pico sdk> ..'")
endif()
include(${PICO_SDK_PATH}/pico_sdk_init.cmake)
set(CMAKE_CXX_STANDARD 20)
project(abort)

pico_sdk_init()

#add_compile_definitions(USE_PRINTF) # For debugging no-os-* library.

add_subdirectory(../../lib/no-OS-FatFS-SD-SDIO-SPI-RPi-Pico/src lib_build/no_os_fatfs_sd_sdio_spi_rpi_pico)
add_subdirectory(../../lib/rp2040.pio-ltc264x lib_build/pio-ltc264x)


add_library(dma_double_buffer INTERFACE)
target_sources(dma_double_buffer INTERFACE
    ../../inc/dma_double_buffer.h
)

add_executable(${PROJECT_NAME}
    src/main.cpp
)

include_directories(../../inc)

target_link_libraries(${PROJECT_NAME}
    pico_stdlib
    pio_ltc264x
    no-OS-FatFS-SD-SDIO-SPI-RPi-Pico
    dma_double_buffer
)

pico_add_extra_outputs(${PROJECT_NAME})

pico_enable_stdio_usb(${PROJECT_NAME} 1)
