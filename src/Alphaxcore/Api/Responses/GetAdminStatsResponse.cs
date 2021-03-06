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

namespace Alphaxcore.Api.Responses
{
    public class AdminGcStats
    {
        /// <summary>
        /// Number of Generation 0 collections
        /// </summary>
        public int GcGen0 { get; set; }

        /// <summary>
        /// Number of Generation 1 collections
        /// </summary>
        public int GcGen1 { get; set; }

        /// <summary>
        /// Number of Generation 2 collections
        /// </summary>
        public int GcGen2 { get; set; }

        /// <summary>
        /// Assumed amount of allocated memory
        /// </summary>
        public string MemAllocated { get; set; }

        /// <summary>
        /// Maximum time in seconds spent in full GC
        /// </summary>
        public double MaxFullGcDuration { get; set; } = 0;
    }
}
