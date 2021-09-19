#ifndef ASTRALHASH_H
#define ASTRALHASH_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdint.h>

void astralhash_hash(const char* input, char* output, uint32_t len);

#ifdef __cplusplus
}
#endif

#endif