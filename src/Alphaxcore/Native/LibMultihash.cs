/*
Copyright 2017 - 2020 Coin Foundry (coinfoundry.org)
Copyright 2020 - 2021 AlphaX Projects (alphax.pro)
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

using System;
using System.Runtime.InteropServices;

namespace Alphaxcore.Native
{
    public static unsafe class LibMultihash
    {
        [DllImport("libmultihash", EntryPoint = "astralhash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int astralhash(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "balloon_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int balloon(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "bcrypt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int bcrypt(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "blake_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int blake(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "blake2s_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int blake2s(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "c11_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int c11(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "cpupower_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int cpupower(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "dcrypt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int dcrypt(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "fresh_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fresh(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "fugue_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int fugue(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "geek_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int geek(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "globalhash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int globalhash(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "groestl_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int groestl(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "groestl_myriad_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int groestl_myriad(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "hefty1_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int hefty1(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "jeonghash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int jeonghash(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "jh_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int jh(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "keccak_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int keccak(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "lyra2re_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lyra2re(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "lyra2rev2_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lyra2rev2(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "lyra2rev3_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lyra2rev3(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "lyra2vc0ban_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lyra2vc0ban(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "lyra2z_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lyra2z(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "lyra2z330_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int lyra2z330(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "neoscrypt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int neoscrypt(byte* input, void* output, uint inputLength, uint profile);
        
        [DllImport("libmultihash", EntryPoint = "nist5_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int nist5(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "odocrypt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int odocrypt(byte* input, void* output, uint inputLength, uint key);
        
        [DllImport("libmultihash", EntryPoint = "padihash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int padihash(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "pawelhash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int pawelhash(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "phi_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int phi(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "phi2_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int phi2(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "phi5_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int phi5(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "power2b_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int power2b(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "quark_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int quark(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "qubit_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int qubit(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "s3_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int s3(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "scrypt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int scrypt(byte* input, void* output, uint n, uint r, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "scryptn_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int scryptn(byte* input, void* output, uint nFactor, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "sha256csm_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sha256csm(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "shavite3_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int shavite3(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "skein_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int skein(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x11_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x11(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x11evo_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x11evo(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x11k_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x11k(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x11kvs_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x11kvs(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x12_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x12(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x13_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x13(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x13_bcd_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x13_bcd(byte* input, void* output);
        
        [DllImport("libmultihash", EntryPoint = "x14_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x14(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x15_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x15(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x16r_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x16r(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x16rt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x16rt(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x16rv2_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x16rv2(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x16s_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x16s(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x17_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x17(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x17r_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x17r(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x18_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x18(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x20r_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x20r(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x21s_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x21s(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x22_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x22(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "x22i_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x22i(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "x25x_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int x25x(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "yescrypt_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yescrypt(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yescryptR8_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yescryptR8(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yescryptR16_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yescryptR16(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yescryptR32_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yescryptR32(byte* input, void* output, uint inputLength);
                
        [DllImport("libmultihash", EntryPoint = "yespower_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_arwn_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_arwn(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "yespower_ic_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_ic(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_iots_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_iots(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_litb_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_litb(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_ltncg_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_ltncg(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_mgpc_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_mgpc(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_r16_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_r16(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_res_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_res(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_sugar_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_sugar(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_tide_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_tide(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "yespower_urx_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int yespower_urx(byte* input, void* output, uint inputLength);
        
        [DllImport("libmultihash", EntryPoint = "equihash_verify_200_9_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool equihash_verify_200_9(byte* header, int headerLength, byte* solution, int solutionLength, string personalization);

        [DllImport("libmultihash", EntryPoint = "equihash_verify_144_5_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool equihash_verify_144_5(byte* header, int headerLength, byte* solution, int solutionLength, string personalization);

        [DllImport("libmultihash", EntryPoint = "equihash_verify_96_5_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool equihash_verify_96_5(byte* header, int headerLength, byte* solution, int solutionLength, string personalization);
        
        [DllImport("libmultihash", EntryPoint = "verushash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int verushash2v2(byte* input, void* output, int inputLength);

        [DllImport("libmultihash", EntryPoint = "sha3_256_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sha3_256(byte* input, void* output, uint inputLength);

        [DllImport("libmultihash", EntryPoint = "sha3_512_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sha3_512(byte* input, void* output, uint inputLength);

        #region Ethash

        [StructLayout(LayoutKind.Sequential)]
        public struct ethash_h256_t
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 32)] public byte[] value;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ethash_return_value
        {
            public ethash_h256_t result;
            public ethash_h256_t mix_hash;

            [MarshalAs(UnmanagedType.U1)] public bool success;
        }

        public delegate int ethash_callback_t(uint progress);

        /// <summary>
        /// Allocate and initialize a new ethash_light handler
        /// </summary>
        /// <param name="block_number">The block number for which to create the handler</param>
        /// <returns>Newly allocated ethash_light handler or NULL</returns>
        [DllImport("libmultihash", EntryPoint = "ethash_light_new_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ethash_light_new(ulong block_number);

        /// <summary>
        /// Frees a previously allocated ethash_light handler
        /// </summary>
        /// <param name="handle">The light handler to free</param>
        [DllImport("libmultihash", EntryPoint = "ethash_light_delete_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ethash_light_delete(IntPtr handle);

        /// <summary>
        /// Calculate the light client data
        /// </summary>
        /// <param name="handle">The light client handler</param>
        /// <param name="header_hash">The 32-Byte header hash to pack into the mix</param>
        /// <param name="nonce">The nonce to pack into the mix</param>
        /// <returns>an object of ethash_return_value_t holding the return values</returns>
        [DllImport("libmultihash", EntryPoint = "ethash_light_compute_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ethash_light_compute(IntPtr handle, byte* header_hash, ulong nonce, ref ethash_return_value result);

        /// <summary>
        /// Allocate and initialize a new ethash_full handler
        /// </summary>
        /// <param name="dagDir">Directory where generated DAGs reside</param>
        /// <param name="light">The light handler containing the cache.</param>
        /// <param name="callback">
        /// A callback function with signature of @ref ethash_callback_t
        /// It accepts an unsigned with which a progress of DAG calculation
        /// can be displayed. If all goes well the callback should return 0.
        /// If a non-zero value is returned then DAG generation will stop.
        /// Be advised. A progress value of 100 means that DAG creation is
        /// almost complete and that this function will soon return succesfully.
        /// It does not mean that the function has already had a succesfull return.
        /// </param>
        /// <returns></returns>
        [DllImport("libmultihash", EntryPoint = "ethash_full_new_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ethash_full_new(string dagDir, IntPtr light, ethash_callback_t callback);

        /// <summary>
        /// Frees a previously allocated ethash_full handler
        /// </summary>
        /// <param name="handle">The full handler to free</param>
        [DllImport("libmultihash", EntryPoint = "ethash_full_delete_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ethash_full_delete(IntPtr handle);

        /// <summary>
        /// Calculate the full client data
        /// </summary>
        /// <param name="handle">The full client handler</param>
        /// <param name="header_hash">The 32-Byte header hash to pack into the mix</param>
        /// <param name="nonce">The nonce to pack into the mix</param>
        /// <returns>an object of ethash_return_value_t holding the return values</returns>
        [DllImport("libmultihash", EntryPoint = "ethash_full_compute_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ethash_full_compute(IntPtr handle, byte* header_hash, ulong nonce, ref ethash_return_value result);

        /// <summary>
        /// Get a pointer to the full DAG data
        /// </summary>
        /// <param name="handle">The full handler to free</param>
        [DllImport("libmultihash", EntryPoint = "ethash_full_dag_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ethash_full_dag(IntPtr handle);

        /// <summary>
        /// Get the size of the DAG data
        /// </summary>
        /// <param name="handle">The full handler to free</param>
        [DllImport("libmultihash", EntryPoint = "ethash_full_dag_size_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong ethash_full_dag_size(IntPtr handle);

        /// <summary>
        /// Calculate the seedhash for a given block number
        /// </summary>
        /// <param name="handle">The full handler to free</param>
        [DllImport("libmultihash", EntryPoint = "ethash_get_seedhash_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern ethash_h256_t ethash_get_seedhash(ulong block_number);

        /// <summary>
        /// Get the default DAG directory
        /// </summary>
        [DllImport("libmultihash", EntryPoint = "ethash_get_default_dirname_export", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ethash_get_default_dirname(byte* data, int length);

        #endregion // Ethash
    }
}
