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
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Alphaxcore.Persistence.Model;
using Alphaxcore.Persistence.Model.Projections;

namespace Alphaxcore.Persistence.Repositories
{
    public interface IShareRepository
    {
        Task InsertAsync(IDbConnection con, IDbTransaction tx, Share share);
        Task BatchInsertAsync(IDbConnection con, IDbTransaction tx, IEnumerable<Share> shares);
        Task<Share[]> ReadSharesBeforeCreatedAsync(IDbConnection con, string poolId, DateTime before, bool inclusive, int pageSize);
        Task<Share[]> ReadSharesBeforeAndAfterCreatedAsync(IDbConnection con, string poolId, DateTime before, DateTime after, bool inclusive, int pageSize);
        Task<Share[]> PageSharesBetweenCreatedAsync(IDbConnection con, string poolId, DateTime start, DateTime end, int page, int pageSize);

        Task<long> CountSharesBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, DateTime before);
        Task DeleteSharesBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, DateTime before);
        Task<long> CountSharesSoloBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, string miner, DateTime before);
        Task DeleteSharesSoloBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, string miner, DateTime before);

        Task<long> CountSharesBetweenCreatedAsync(IDbConnection con, string poolId, string miner, DateTime? start, DateTime? end);
        Task<double?> GetAccumulatedShareDifficultyBetweenCreatedAsync(IDbConnection con, string poolId, DateTime start, DateTime end);
        Task<MinerWorkerHashes[]> GetAccumulatedShareDifficultyTotalAsync(IDbConnection con, string poolId);
        Task<MinerWorkerHashes[]> GetHashAccumulationBetweenCreatedAsync(IDbConnection con, string poolId, DateTime start, DateTime end);
    }
}
