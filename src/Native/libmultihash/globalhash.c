#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stdio.h>

#include "globalhash.h"
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


void globalhash_hash(const char* input, char* output, uint32_t len)
{
    sph_gost512_context      ctx_gost;
    sph_blake512_context     ctx_blake;
    blake2b_state            ctx_blake2b[1];
    blake2s_state            ctx_blake2s[1];
    
    //these uint512 in the c++ source of the client are backed by an array of uint32
    uint32_t hashA[16], hashB[16], finalhash[8]; // finalhash is a 256 unsigned integer
    
    sph_gost512_init(&ctx_gost);
    sph_gost512 (&ctx_gost, input, len); 
    sph_gost512_close(&ctx_gost, hashA);
    
    sph_blake512_init(&ctx_blake);
    sph_blake512(&ctx_blake, hashA, 64);
    sph_blake512_close(&ctx_blake, hashB);
    
    blake2b_init( ctx_blake2b, BLAKE2B_OUTBYTES );
    blake2b_update( ctx_blake2b, hashB, 64 );
    blake2b_final( ctx_blake2b, hashA, BLAKE2B_OUTBYTES );
    
    blake2s_init( ctx_blake2s, BLAKE2S_OUTBYTES );
    blake2s_update( ctx_blake2s, hashA, 64);
    blake2s_final( ctx_blake2s, finalhash, BLAKE2S_OUTBYTES );

    memcpy(output, finalhash, 32);
}