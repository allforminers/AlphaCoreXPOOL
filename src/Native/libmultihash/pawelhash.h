#ifndef PAWELHASH_H
#define PAWELHASH_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdint.h>

void pawelhash_hash(const char* input, char* output, uint32_t len);

#ifdef __cplusplus
}
#endif

#endif