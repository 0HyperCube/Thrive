# Native code side of Thrive
cmake_minimum_required(VERSION 3.10)

project(Thrive)

# set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${PROJECT_SOURCE_DIR}/CMake")

# static building
SET(CMAKE_FIND_LIBRARY_SUFFIXES ".a")
SET(BUILD_SHARED_LIBS OFF)

# Common options
if(CMAKE_BUILD_TYPE STREQUAL "")
  set(CMAKE_BUILD_TYPE Release CACHE STRING
    "Set the build type, usually Release or RelWithDebInfo" FORCE)
endif(CMAKE_BUILD_TYPE STREQUAL "")

# # Configure include file
# configure_file("src/Include.h.in" "${PROJECT_BINARY_DIR}/Include.h")

# include_directories(${PROJECT_BINARY_DIR})

add_subdirectory(third_party)


add_subdirectory(src/native)
