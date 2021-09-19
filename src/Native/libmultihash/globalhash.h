#ifndef GLOBALHASH_H
#define GLOBALHASH_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdint.h>

void globalhash_hash(const char* input, char* output, uint32_t len);

#ifdef __cplusplus
}
#endif

#endif