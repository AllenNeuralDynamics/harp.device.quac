cmake_minimum_required(VERSION 3.13)
find_package(Git REQUIRED)
execute_process(COMMAND "${GIT_EXECUTABLE}" rev-parse --short HEAD OUTPUT_VARIABLE COMMIT_ID OUTPUT_STRIP_TRAILING_WHITESPACE)
message(STATUS "Computed Git Hash: ${COMMIT_ID}")
add_definitions(-DGIT_HASH="${COMMIT_ID}") # Usable in source code.

#add_definitions(-DDEBUG) # Uncomment for debugging

set(PICO_BOARD pico2)
set(PICO_PLATFORM rp2350-arm-s)

# Specify USB Manufacturer and Product descriptions.
add_definitions(-DUSBD_MANUFACTURER="Allen Institute")
add_definitions(-DUSBD_PRODUCT="Quad DAC")

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
pico_sdk_init()

set(CMAKE_CXX_STANDARD 20)

project(quac)


add_compile_definitions(USE_PRINTF)

add_subdirectory(lib/no-OS-FatFS-SD-SDIO-SPI-RPi-Pico/src lib_build/no_os_fatfs_sd_sdio_spi_rpi_pico)
add_subdirectory(lib/core.pico/firmware lib_build/pico-core)
add_subdirectory(lib/rp2040.pio-ltc264x lib_build/pio-ltc264x)

add_executable(${PROJECT_NAME}
    src/main.cpp
    src/sd_hw_config.cpp
)

include_directories(inc)

target_link_libraries(${PROJECT_NAME}
    harp_c_app
    harp_sync
    pico_stdlib
    pio_ltc264x
    #hardware_adc
    hardware_dma
    no-OS-FatFS-SD-SDIO-SPI-RPi-Pico

)

pico_add_extra_outputs(${PROJECT_NAME})

if(DEBUG)
    message(WARNING "Debug printf() messages from harp core to UART with baud \
            rate 921600.")
    pico_enable_stdio_uart(${PROJECT_NAME} 1) # UART stdio for printf.
    # Additional libraries need to have stdio init also.
endif()

