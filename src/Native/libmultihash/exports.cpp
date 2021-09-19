/*
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)
         Olaf Wasilewski (olaf.wasilewski@gmx.de)
         
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#include "astralhash.h"
#include "balloon.h"
#include "bcrypt.h"
#include "blake.h"
#include "blake2s.h"
#include "c11.h"
#include "dcrypt.h"
#include "fresh.h"
#include "fugue.h"
#include "geek.h"
#include "globalhash.h"
#include "groestl.h"
#include "hashodo.h"
#include "hefty1.h"
#include "jeonghash.h"
#include "jh.h"
#include "keccak.h"
#include "Lyra2.h"
#include "Lyra2-z.h"
#include "lyra2re.h"
#include "lyra2v2.h"
#include "lyra2v3.h"
#include "lyra2vc0ban.h"
#include "lyra2z.h"
#include "lyra2z330.h"
#include "neoscrypt.h"
#include "nist5.h"
#include "padihash.h"
#include "pawelhash.h"
#include "phi.h"
#include "phi2.h"
#include "phi5.h"
#include "quark.h"
#include "qubit.h"
#include "s3.h"
#include "scryptjane.h"
#include "scryptn.h"
#include "sha256csm.h"
#include "shavite3.h"
#include "skein.h"
#include "x11.h"
#include "x11evo.h"
#include "x11k.h"
#include "x11kvs.h"
#include "x12.h"
#include "x13.h"
#include "x14.h"
#include "x15.h"
#include "x16r.h"
#include "x16rt.h"
#include "x16rv2.h"
#include "x16s.h"
#include "x17.h"
#include "x17r.h"
#include "x18.h"
#include "x20r.h"
#include "x21s.h"
#include "x22.h"
#include "x22i.h"
#include "x25x.h"
#include "yescrypt.h"
#include "equi/equihashverify.h"
#include "libethash/sha3.h"
#include "libethash/internal.h"
#include "libethash/ethash.h"
#include "verushash/verus_hash.h"
#include "yespower/yespower.h"

extern "C" bool ethash_get_default_dirname(char* strbuf, size_t buffsize);

#ifdef _WIN32
#define MODULE_API __declspec(dllexport)
#else
#define MODULE_API
#endif

extern "C" MODULE_API void astralhash_export(const char* input, char* output, uint32_t input_len)
{
	astralhash_hash(input, output, input_len);
}

extern "C" MODULE_API void balloon_export(const char* input, char* output, uint32_t input_len)
{
	balloon(input, output, input_len);
}

extern "C" MODULE_API void bcrypt_export(const char* input, char* output, uint32_t input_len)
{
	bcrypt_hash(input, output);
}

extern "C" MODULE_API void blake_export(const char* input, char* output, uint32_t input_len)
{
	blake_hash(input, output, input_len);
}

extern "C" MODULE_API void blake2s_export(const char* input, char* output, uint32_t input_len)
{
    blake2s_hash(input, output, input_len);
}

extern "C" MODULE_API void c11_export(const char* input, char* output)
{
	c11_hash(input, output);
}

extern "C" MODULE_API void cpupower_export(const char* input, char* output, uint32_t input_len)
{
    cpupower_hash(input, output, input_len);
}

extern "C" MODULE_API void dcrypt_export(const char* input, char* output, uint32_t input_len)
{
	dcrypt_hash(input, output, input_len);
}

extern "C" MODULE_API void fresh_export(const char* input, char* output, uint32_t input_len)
{
	fresh_hash(input, output, input_len);
}

extern "C" MODULE_API void fugue_export(const char* input, char* output, uint32_t input_len)
{
	fugue_hash(input, output, input_len);
}

extern "C" MODULE_API void geek_export(const char* input, char* output, uint32_t input_len)
{
	geek_hash(input, output, input_len);
}

extern "C" MODULE_API void globalhash_export(const char* input, char* output, uint32_t input_len)
{
	globalhash_hash(input, output, input_len);
}

extern "C" MODULE_API void groestl_export(const char* input, char* output, uint32_t input_len)
{
	groestl_hash(input, output, input_len);
}

extern "C" MODULE_API void groestl_myriad_export(const char* input, char* output, uint32_t input_len)
{
	groestlmyriad_hash(input, output, input_len);
}

extern "C" MODULE_API void hefty1_export(const char* input, char* output, uint32_t input_len)
{
	hefty1_hash(input, output, input_len);
}

extern "C" MODULE_API void jeonghash_export(const char* input, char* output, uint32_t input_len)
{
	jeonghash_hash(input, output, input_len);
}

extern "C" MODULE_API void jh_export(const char* input, char* output, uint32_t input_len)
{
	jh_hash(input, output, input_len);
}

extern "C" MODULE_API void keccak_export(const char* input, char* output, uint32_t input_len)
{
	keccak_hash(input, output, input_len);
}

extern "C" MODULE_API void lyra2re_export(const char* input, char* output, uint32_t input_len)
{
	lyra2re_hash(input, output, input_len);
}

extern "C" MODULE_API void lyra2rev2_export(const char* input, char* output, uint32_t input_len)
{
	lyra2v2_hash(input, output, input_len);
}

extern "C" MODULE_API void lyra2rev3_export(const char* input, char* output, uint32_t input_len)
{
	lyra2v3_hash(input, output, input_len);
}

extern "C" MODULE_API void lyra2vc0ban_export(const char* input, char* output, uint32_t input_len)
{
	lyra2vc0ban_hash(input, output, input_len);
}

extern "C" MODULE_API void lyra2z_export(const char* input, char* output, uint32_t input_len)
{
	lyra2z_hash(input, output, input_len);
}

extern "C" MODULE_API void lyra2z330_export(const char* input, char* output, uint32_t input_len)
{
	lyra2z330_hash(input, output, input_len);
}

extern "C" MODULE_API void neoscrypt_export(const unsigned char* input, unsigned char* output, uint32_t profile)
{
	neoscrypt(input, output, profile);
}

extern "C" MODULE_API void nist5_export(const char* input, char* output, uint32_t input_len)
{
	nist5_hash(input, output, input_len);
}

extern "C" MODULE_API void odocrypt_export(const char* input, char* output, uint32_t input_len, uint32_t key)
{
    odocrypt_hash(input, output, input_len, key);
}

extern "C" MODULE_API void padihash_export(const char* input, char* output, uint32_t input_len)
{
	padihash_hash(input, output, input_len);
}

extern "C" MODULE_API void pawelhash_export(const char* input, char* output, uint32_t input_len)
{
	pawelhash_hash(input, output, input_len);
}

extern "C" MODULE_API void phi_export(const char* input, char* output, uint32_t input_len)
{
	phi_hash(input, output, input_len);
}

extern "C" MODULE_API void phi2_export(const char* input, char* output, uint32_t input_len)
{
	phi2_hash(input, output, input_len);
}

extern "C" MODULE_API void phi5_export(const char* input, char* output, uint32_t input_len)
{
	phi5_hash(input, output, input_len);
}

extern "C" MODULE_API void power2b_export(const char* input, char* output, uint32_t input_len)
{
    power2b_hash(input, output, input_len);
}

extern "C" MODULE_API void quark_export(const char* input, char* output, uint32_t input_len)
{
	quark_hash(input, output, input_len);
}

extern "C" MODULE_API void qubit_export(const char* input, char* output, uint32_t input_len)
{
	qubit_hash(input, output, input_len);
}

extern "C" MODULE_API void s3_export(const char* input, char* output, uint32_t input_len)
{
	s3_hash(input, output, input_len);
}

extern "C" MODULE_API void scrypt_export(const char* input, char* output, uint32_t N, uint32_t R, uint32_t input_len)
{
	scrypt_N_R_1_256(input, output, N, R, input_len);
}

extern "C" MODULE_API void scryptn_export(const char* input, char* output, uint32_t nFactor, uint32_t input_len)
{
	unsigned int N = 1 << nFactor;

	scrypt_N_R_1_256(input, output, N, 1, input_len); //hardcode for now to R=1 for now
}

extern "C" MODULE_API void sha256csm_export(const char* input, char* output, uint32_t input_len)
{
	sha256csm_hash(input, output, input_len);
}

extern "C" MODULE_API void shavite3_export(const char* input, char* output, uint32_t input_len)
{
	shavite3_hash(input, output, input_len);
}

extern "C" MODULE_API void skein_export(const char* input, char* output, uint32_t input_len)
{
	skein_hash(input, output, input_len);
}

extern "C" MODULE_API void x11_export(const char* input, char* output, uint32_t input_len)
{
	x11_hash(input, output, input_len);
}

extern "C" MODULE_API void x11evo_export(const char* input, char* output, uint32_t input_len)
{
	x11evo_hash(input, output, input_len);
}

extern "C" MODULE_API void x11k_export(const char* input, char* output, uint32_t input_len)
{
	x11k_hash(input, output, input_len);
}

extern "C" MODULE_API void x11kvs_export(const char* input, char* output, uint32_t input_len)
{
	x11kvs_hash(input, output, input_len);
}

extern "C" MODULE_API void x12_export(const char* input, char* output, uint32_t input_len)
{
	x12_hash(input, output, input_len);
}

extern "C" MODULE_API void x13_export(const char* input, char* output, uint32_t input_len)
{
    x13_hash(input, output, input_len);
}

extern "C" MODULE_API void x13_bcd_export(const char* input, char* output)
{
    x13_bcd_hash(input, output);
}

extern "C" MODULE_API void x14_export(const char* input, char* output, uint32_t input_len)
{
    x14_hash(input, output, input_len);
}

extern "C" MODULE_API void x15_export(const char* input, char* output, uint32_t input_len)
{
	x15_hash(input, output, input_len);
}

extern "C" MODULE_API void x16r_export(const char* input, char* output, uint32_t input_len)
{
    x16r_hash(input, output, input_len);
}

extern "C" MODULE_API void x16rt_export(const char* input, char* output, uint32_t input_len)
{
    x16rt_hash(input, output, input_len);
}

extern "C" MODULE_API void x16rv2_export(const char* input, char* output, uint32_t input_len)
{
    x16rv2_hash(input, output,input_len);
}

extern "C" MODULE_API void x16s_export(const char* input, char* output, uint32_t input_len)
{
    x16s_hash(input, output, input_len);
}

extern "C" MODULE_API void x17_export(const char* input, char* output, uint32_t input_len)
{
    x17_hash(input, output, input_len);
}

extern "C" MODULE_API void x17r_export(const char* input, char* output, uint32_t input_len)
{
    x17r_hash(input, output, input_len);
}

extern "C" MODULE_API void x18_export(const char* input, char* output, uint32_t input_len)
{
    x18_hash(input, output, input_len);
}

extern "C" MODULE_API void x20r_export(const char* input, char* output, uint32_t input_len)
{
    x20r_hash(input, output, input_len);
}

extern "C" MODULE_API void x21s_export(const char* input, char* output, uint32_t input_len)
{
	x21s_hash(input, output, input_len);
}

extern "C" MODULE_API void x22_export(const char* input, char* output, uint32_t input_len)
{
    x22_hash(input, output, input_len);
}

extern "C" MODULE_API void x22i_export(const char* input, char* output, uint32_t input_len)
{
    x22i_hash(input, output, input_len);
}

extern "C" MODULE_API void x25x_export(const char* input, char* output, uint32_t input_len)
{
    x25x_hash(input, output, input_len);
}

extern "C" MODULE_API void yescrypt_export(const char* input, char* output, uint32_t input_len)
{
    yescrypt_hash(input, output, input_len);
}

extern "C" MODULE_API void yescryptR8_export(const char* input, char* output, uint32_t input_len)
{
    yescryptR8_hash(input, output, input_len);
}

extern "C" MODULE_API void yescryptR16_export(const char* input, char* output, uint32_t input_len)
{
    yescryptR16_hash(input, output, input_len);
}

extern "C" MODULE_API void yescryptR32_export(const char* input, char* output, uint32_t input_len)
{
    yescryptR32_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_export(const char* input, char* output, uint32_t input_len)
{
    yespower_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_ic_export(const char* input, char* output, uint32_t input_len)
{
    yespowerIC_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_arwn_export(const char* input, char* output, uint32_t input_len)
{
    yespowerARWN_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_iots_export(const char* input, char* output, uint32_t input_len)
{
    yespowerIOTS_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_litb_export(const char* input, char* output, uint32_t input_len)
{
    yespowerLITB_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_ltncg_export(const char* input, char* output, uint32_t input_len)
{
    yespowerLTNCG_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_mgpc_export(const char* input, char* output, uint32_t input_len)
{
    yespowerMGPC_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_r16_export(const char* input, char* output, uint32_t input_len)
{
    yespowerR16_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_res_export(const char* input, char* output, uint32_t input_len)
{
    yespowerRES_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_sugar_export(const char* input, char* output, uint32_t input_len)
{
    yespowerSUGAR_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_tide_export(const char* input, char* output, uint32_t input_len)
{
    yespowerTIDE_hash(input, output, input_len);
}

extern "C" MODULE_API void yespower_urx_export(const char* input, char* output, uint32_t input_len)
{
    yespowerURX_hash(input, output, input_len);
}

extern "C" MODULE_API bool equihash_verify_200_9_export(const char* header, int header_length, const char* solution, int solution_length, const char *personalization)
{
    if (header_length != 140) {
        return false;
    }

    std::vector<unsigned char> vecSolution(solution, solution + solution_length);

    return verifyEH_200_9(header, vecSolution, personalization);
}

extern "C" MODULE_API bool equihash_verify_144_5_export(const char* header, int header_length, const char* solution, int solution_length, const char *personalization)
{
    if (header_length != 140) {
        return false;
    }

    std::vector<unsigned char> vecSolution(solution, solution + solution_length);

    return verifyEH_144_5(header, vecSolution, personalization);
}

extern "C" MODULE_API bool equihash_verify_96_5_export(const char* header, int header_length, const char* solution, int solution_length, const char *personalization)
{
    if (header_length != 140) {
        return false;
    }

    std::vector<unsigned char> vecSolution(solution, solution + solution_length);

    return verifyEH_96_5(header, vecSolution, personalization);
}

extern "C" MODULE_API void sha3_256_export(const char* input, char* output, uint32_t input_len)
{
	SHA3_256((ethash_h256 const*) output, (uint8_t const*) input, input_len);
}

extern "C" MODULE_API void sha3_512_export(const char* input, char* output, uint32_t input_len)
{
	SHA3_512((uint8_t*) output, (uint8_t const*)input, input_len);
}

extern "C" MODULE_API uint64_t ethash_get_datasize_export(uint64_t const block_number)
{
	return ethash_get_datasize(block_number);
}

extern "C" MODULE_API uint64_t ethash_get_cachesize_export(uint64_t const block_number)
{
	return ethash_get_cachesize(block_number);
}

extern "C" MODULE_API ethash_light_t ethash_light_new_export(uint64_t block_number)
{
	return ethash_light_new(block_number);
}

extern "C" MODULE_API void ethash_light_delete_export(ethash_light_t light)
{
	ethash_light_delete(light);
}

extern "C" MODULE_API void ethash_light_compute_export(
	ethash_light_t light,
	ethash_h256_t const *header_hash,
	uint64_t nonce,
	ethash_return_value_t *result)
{
	*result = ethash_light_compute(light, *header_hash, nonce);
}

extern "C" MODULE_API ethash_full_t ethash_full_new_export(const char *dirname, ethash_light_t light, ethash_callback_t callback)
{
	uint64_t full_size = ethash_get_datasize(light->block_number);
	ethash_h256_t seedhash = ethash_get_seedhash(light->block_number);
	return ethash_full_new_internal(dirname, seedhash, full_size, light, callback);
}

extern "C" MODULE_API void ethash_full_delete_export(ethash_full_t full)
{
	ethash_full_delete(full);
}

extern "C" MODULE_API void ethash_full_compute_export(
	ethash_full_t full,
	ethash_h256_t const *header_hash,
	uint64_t nonce,
	ethash_return_value_t *result)
{
	*result = ethash_full_compute(full, *header_hash, nonce);
}

extern "C" MODULE_API void const* ethash_full_dag_export(ethash_full_t full)
{
	return ethash_full_dag(full);
}

extern "C" MODULE_API uint64_t ethash_full_dag_size_export(ethash_full_t full)
{
	return ethash_full_dag_size(full);
}

extern "C" MODULE_API ethash_h256_t ethash_get_seedhash_export(uint64_t block_number)
{
	return ethash_get_seedhash(block_number);
}

extern "C" MODULE_API bool ethash_get_default_dirname_export(char *buf, size_t buf_size)
{
	return ethash_get_default_dirname(buf, buf_size);
}

extern "C" MODULE_API void verushash_export(const char* input, char* output, int input_len)
{
    CVerusHashV2* vh2b2;
    CVerusHashV2::init();
    vh2b2 = new CVerusHashV2(SOLUTION_VERUSHHASH_V2_2);
    vh2b2->Reset();
    vh2b2->Write((const unsigned char *)input, input_len);
    vh2b2->Finalize2b((unsigned char *)output);
}
