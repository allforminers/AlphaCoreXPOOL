#ifndef PADIHASH_H
#define PADIHASH_H

#ifdef __cplusplus
extern "C" {
#endif

#include <stdint.h>

void padihash_hash(const char* input, char* output, uint32_t len);

#ifdef __cplusplus
}
#endif

#endif