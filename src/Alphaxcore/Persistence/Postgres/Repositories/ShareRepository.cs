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
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dapper;
using Alphaxcore.Extensions;
using Alphaxcore.Persistence.Model;
using Alphaxcore.Persistence.Model.Projections;
using Alphaxcore.Persistence.Repositories;
using Alphaxcore.Util;
using NLog;
using Npgsql;
using NpgsqlTypes;

namespace Alphaxcore.Persistence.Postgres.Repositories
{
    public class ShareRepository : IShareRepository
    {
        public ShareRepository(IMapper mapper)
        {
            this.mapper = mapper;
        }

        private readonly IMapper mapper;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public async Task InsertAsync(IDbConnection con, IDbTransaction tx, Share share)
        {
            logger.LogInvoke();

            var mapped = mapper.Map<Entities.Share>(share);

            const string query = "INSERT INTO shares(poolid, blockheight, difficulty, " +
                "networkdifficulty, miner, worker, useragent, ipaddress, source, created) " +
                "VALUES(@poolid, @blockheight, @difficulty, " +
                "@networkdifficulty, @miner, @worker, @useragent, @ipaddress, @source, @created)";

            await con.ExecuteAsync(query, mapped, tx);
        }

        public Task BatchInsertAsync(IDbConnection con, IDbTransaction tx, IEnumerable<Share> shares)
        {
            logger.LogInvoke();

            // NOTE: Even though the tx parameter is completely ignored here,
            // the COPY command still honors a current ambient transaction

            var pgCon = (NpgsqlConnection) con;

            const string query = "COPY shares (poolid, blockheight, difficulty, " +
                "networkdifficulty, miner, worker, useragent, ipaddress, source, created) FROM STDIN (FORMAT BINARY)";

            using(var writer = pgCon.BeginBinaryImport(query))
            {
                foreach(var share in shares)
                {
                    writer.StartRow();

                    writer.Write(share.PoolId);
                    writer.Write((long) share.BlockHeight, NpgsqlDbType.Bigint);
                    writer.Write(share.Difficulty, NpgsqlDbType.Double);
                    writer.Write(share.NetworkDifficulty, NpgsqlDbType.Double);
                    writer.Write(share.Miner);
                    writer.Write(share.Worker);
                    writer.Write(share.UserAgent);
                    writer.Write(share.IpAddress);
                    writer.Write(share.Source);
                    writer.Write(share.Created, NpgsqlDbType.Timestamp);
                }

                writer.Complete();
            }

            return Task.FromResult(true);
        }

        public async Task<Share[]> ReadSharesBeforeCreatedAsync(IDbConnection con, string poolId, DateTime before, bool inclusive, int pageSize)
        {
            logger.LogInvoke(new[] { poolId });

            var query = $"SELECT * FROM shares WHERE poolid = @poolId AND created {(inclusive ? " <= " : " < ")} @before " +
                "ORDER BY created DESC FETCH NEXT (@pageSize) ROWS ONLY";

            return (await con.QueryAsync<Entities.Share>(query, new { poolId, before, pageSize }))
                .Select(mapper.Map<Share>)
                .ToArray();
        }

        public async Task<Share[]> ReadSharesBeforeAndAfterCreatedAsync(IDbConnection con, string poolId, DateTime before, DateTime after, bool inclusive, int pageSize)
        {
            logger.LogInvoke(new[] { poolId });

            var query = $"SELECT * FROM shares WHERE poolid = @poolId AND created {(inclusive ? " <= " : " < ")} @before " +
                $"AND created {(inclusive ? " >= " : " > ")} @after" +
                "ORDER BY created DESC FETCH NEXT (@pageSize) ROWS ONLY";

            return (await con.QueryAsync<Entities.Share>(query, new { poolId, before, after, pageSize }))
                .Select(mapper.Map<Share>)
                .ToArray();
        }

        public async Task<Share[]> PageSharesBetweenCreatedAsync(IDbConnection con, string poolId, DateTime start, DateTime end, int page, int pageSize)
        {
            logger.LogInvoke(new[] { poolId });

            var query = "SELECT * FROM shares WHERE poolid = @poolId AND created >= @start AND created <= @end " +
                "ORDER BY created DESC OFFSET @offset FETCH NEXT (@pageSize) ROWS ONLY";

            return (await con.QueryAsync<Entities.Share>(query, new { poolId, start, end, offset = page * pageSize, pageSize }))
                .Select(mapper.Map<Share>)
                .ToArray();
        }

        public Task<long> CountSharesBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, DateTime before)
        {
            logger.LogInvoke(new[] { poolId });

            const string query = "SELECT count(*) FROM shares WHERE poolid = @poolId AND created < @before";

            return con.QuerySingleAsync<long>(query, new { poolId, before }, tx);
        }

        public async Task DeleteSharesBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, DateTime before)
        {
            logger.LogInvoke(new[] { poolId });

            const string query = "DELETE FROM shares WHERE poolid = @poolId AND created < @before";

            await con.ExecuteAsync(query, new { poolId, before }, tx);
        }
           
        public Task<long> CountSharesSoloBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, string miner, DateTime before)
        {
            logger.LogInvoke(new[] { poolId });

            const string query = "SELECT count(*) FROM shares WHERE poolid = @poolId AND miner = @miner";

            return con.QuerySingleAsync<long>(query, new { poolId, miner}, tx);
        }

        public async Task DeleteSharesSoloBeforeCreatedAsync(IDbConnection con, IDbTransaction tx, string poolId, string miner, DateTime before)
        {
            logger.LogInvoke(new[] { poolId });

            const string query = "DELETE FROM shares WHERE poolid = @poolId AND miner = @miner";

            await con.ExecuteAsync(query, new { poolId, miner}, tx);
        }

        public Task<long> CountSharesBetweenCreatedAsync(IDbConnection con, string poolId, string miner, DateTime? start, DateTime? end)
        {
            logger.LogInvoke(new[] { poolId });

            var whereClause = "poolid = @poolId AND miner = @miner";

            if(start.HasValue)
                whereClause += " AND created >= @start ";
            if(end.HasValue)
                whereClause += " AND created <= @end";

            var query = $"SELECT count(*) FROM shares WHERE {whereClause}";

            return con.QuerySingleAsync<long>(query, new { poolId, miner, start, end });
        }

        public Task<double?> GetAccumulatedShareDifficultyBetweenCreatedAsync(IDbConnection con, string poolId, DateTime start, DateTime end)
        {
            logger.LogInvoke(new[] { poolId });

            const string query = "SELECT SUM(difficulty) FROM shares WHERE poolid = @poolId AND created > @start AND created < @end";

            return con.QuerySingleAsync<double?>(query, new { poolId, start, end });
        }

        public async Task<MinerWorkerHashes[]> GetAccumulatedShareDifficultyTotalAsync(IDbConnection con, string poolId)
        {
            logger.LogInvoke(new[] { (object) poolId });

            const string query = "SELECT SUM(difficulty) AS sum, COUNT(difficulty) AS count, miner, worker FROM shares WHERE poolid = @poolid group by miner, worker";

            return (await con.QueryAsync<MinerWorkerHashes>(query, new { poolId }))
                .ToArray();
        }

        public async Task<MinerWorkerHashes[]> GetHashAccumulationBetweenCreatedAsync(IDbConnection con, string poolId, DateTime start, DateTime end)
        {
            logger.LogInvoke(new[] { poolId });

            const string query = "SELECT SUM(difficulty), COUNT(difficulty), MIN(created) AS firstshare, MAX(created) AS lastshare, miner, worker FROM shares " +
                "WHERE poolid = @poolId AND created >= @start AND created <= @end " +
                "GROUP BY miner, worker";

            return (await con.QueryAsync<MinerWorkerHashes>(query, new { poolId, start, end }))
                .ToArray();
        }
    }
}
