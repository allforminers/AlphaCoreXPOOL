#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stdio.h>

#include "jeonghash.h"
#include "blake2-ref/blake2.h"
#include "sha3/sph_blake.h"
#include "sha3/sph_bmw.h"
#include "sha3/sph_groestl.h"
#include "sha3/sph_jh.h"
#include "sha3/sph_keccak.h"
#include "sha3/sph_skein.h"
#include "sha3/sph_luffa.h"
#include "sha3/sph_cubehash.h"
#include "sha3/sph_shavite.h"
#include "sha3/sph_simd.h"
#include "sha3/sph_echo.h"
#include "sha3/sph_hamsi.h"
#include "sha3/sph_fugue.h"
#include "sha3/sph_shabal.h"
#include "sha3/sph_whirlpool.h"
#include "sha3/sph_sha2.h"
#include "sha3/sph_haval.h"
#include "sha3/sph_gost.h"


void jeonghash_hash(const char* input, char* output, uint32_t len)
{
    sph_simd512_context      ctx_simd;
    sph_hamsi512_context     ctx_hamsi;
    sph_shabal512_context    ctx_shabal;
    sph_blake512_context     ctx_blake;
    sph_bmw512_context       ctx_bmw;
    sph_sha512_context       ctx_sha2;
    sph_whirlpool_context    ctx_whirlpool;
    sph_skein512_context     ctx_skein;

    //these uint512 in the c++ source of the client are backed by an array of uint32
    uint32_t hashA[16], hashB[16];

    sph_simd512_init(&ctx_simd);
    sph_simd512(&ctx_simd, input, len);
    sph_simd512_close(&ctx_simd, hashA);

    sph_hamsi512_init(&ctx_hamsi);
    sph_hamsi512(&ctx_hamsi, hashA, 64);
    sph_hamsi512_close(&ctx_hamsi, hashB);
    
    sph_shabal512_init(&ctx_shabal);
    sph_shabal512(&ctx_shabal, hashB, 64);
    sph_shabal512_close(&ctx_shabal, hashA);
    
    sph_blake512_init(&ctx_blake);
    sph_blake512(&ctx_blake, hashA, 64);
    sph_blake512_close(&ctx_blake, hashB);
    
    sph_bmw512_init(&ctx_bmw);
    sph_bmw512(&ctx_bmw, hashB, 64);
    sph_bmw512_close(&ctx_bmw, hashA);
    
    sph_sha512_init(&ctx_sha2);
    sph_sha512(&ctx_sha2, hashA, 64);
    sph_sha512_close(&ctx_sha2, hashB);
    
    sph_whirlpool_init(&ctx_whirlpool);
    sph_whirlpool(&ctx_whirlpool, hashB, 64);
    sph_whirlpool_close(&ctx_whirlpool, hashA);
    
    sph_skein512_init(&ctx_skein);
    sph_skein512(&ctx_skein, hashA, 64);
    sph_skein512_close(&ctx_skein, hashB);
    
    sph_skein512_init(&ctx_skein);
    sph_skein512(&ctx_skein, hashB, 64);
    sph_skein512_close(&ctx_skein, hashA);
    
    sph_whirlpool_init(&ctx_whirlpool);
    sph_whirlpool(&ctx_whirlpool, hashA, 64);
    sph_whirlpool_close(&ctx_whirlpool, hashB);
    
    sph_sha512_init(&ctx_sha2);
    sph_sha512(&ctx_sha2, hashB, 64);
    sph_sha512_close(&ctx_sha2, hashA);
    
    sph_bmw512_init(&ctx_bmw);
    sph_bmw512(&ctx_bmw, hashA, 64);
    sph_bmw512_close(&ctx_bmw, hashB);
    
    sph_blake512_init(&ctx_blake);
    sph_blake512(&ctx_blake, hashB, 64);
    sph_blake512_close(&ctx_blake, hashA);
    
    sph_shabal512_init(&ctx_shabal);
    sph_shabal512(&ctx_shabal, hashA, 64);
    sph_shabal512_close(&ctx_shabal, hashB);
    
    sph_hamsi512_init(&ctx_hamsi);
    sph_hamsi512(&ctx_hamsi, hashB, 64);
    sph_hamsi512_close(&ctx_hamsi, hashA);
    
    sph_simd512_init(&ctx_simd);
    sph_simd512(&ctx_simd, hashA, 64);
    sph_simd512_close(&ctx_simd, hashB);
    
    sph_simd512_init(&ctx_simd);
    sph_simd512(&ctx_simd, hashB, 64);
    sph_simd512_close(&ctx_simd, hashA);

    sph_hamsi512_init(&ctx_hamsi);
    sph_hamsi512(&ctx_hamsi, hashA, 64);
    sph_hamsi512_close(&ctx_hamsi, hashB);
    
    sph_shabal512_init(&ctx_shabal);
    sph_shabal512(&ctx_shabal, hashB, 64);
    sph_shabal512_close(&ctx_shabal, hashA);
    
    sph_blake512_init(&ctx_blake);
    sph_blake512(&ctx_blake, hashA, 64);
    sph_blake512_close(&ctx_blake, hashB);
    
    sph_bmw512_init(&ctx_bmw);
    sph_bmw512(&ctx_bmw, hashB, 64);
    sph_bmw512_close(&ctx_bmw, hashA);
    
    sph_sha512_init(&ctx_sha2);
    sph_sha512(&ctx_sha2, hashA, 64);
    sph_sha512_close(&ctx_sha2, hashB);
    
    sph_whirlpool_init(&ctx_whirlpool);
    sph_whirlpool(&ctx_whirlpool, hashB, 64);
    sph_whirlpool_close(&ctx_whirlpool, hashA);
    
    sph_skein512_init(&ctx_skein);
    sph_skein512(&ctx_skein, hashA, 64);
    sph_skein512_close(&ctx_skein, hashB);

    memcpy(output, hashB, 32);
}