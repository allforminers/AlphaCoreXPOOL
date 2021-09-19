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

using Alphaxcore.Blockchain;
using Alphaxcore.Messaging;
using Alphaxcore.Persistence.Model;
using System.Globalization;
using Alphaxcore.Notifications.Messages;
using Alphaxcore.Configuration;

namespace Alphaxcore.Extensions
{
    public static class MessageBusExtensions
    {
        public static void NotifyBlockFound(this IMessageBus messageBus, string poolId, Block block, CoinTemplate coin)
        {
            // miner account explorer link
            string minerExplorerLink = null;

            if(!string.IsNullOrEmpty(coin.ExplorerAccountLink))
                minerExplorerLink = string.Format(coin.ExplorerAccountLink, block.Miner);

            messageBus.SendMessage(new BlockFoundNotification
            {
                PoolId = poolId,
                BlockHeight = block.BlockHeight,
                Symbol = coin.Symbol,
                Miner = block.Miner,
                MinerExplorerLink = minerExplorerLink,
                Source = block.Source,
            });
        }

        public static void NotifyBlockConfirmationProgress(this IMessageBus messageBus, string poolId, Block block, CoinTemplate coin)
        {
            messageBus.SendMessage(new BlockConfirmationProgressNotification
            {
                PoolId = poolId,
                BlockHeight = block.BlockHeight,
                Symbol = coin.Symbol,
                Effort = block.Effort,
                Progress = block.ConfirmationProgress,
            });
        }

        public static void NotifyBlockUnlocked(this IMessageBus messageBus, string poolId, Block block, CoinTemplate coin)
        {
            // build explorer link
            string blockExplorerLink = null;
            string minerExplorerLink = null;

            if(block.Status != BlockStatus.Orphaned)
            {
                // block explorer link
                if(coin.ExplorerBlockLinks.TryGetValue(!string.IsNullOrEmpty(block.Type) ? block.Type : "block", out var blockInfobaseUrl))
                {
                    if(blockInfobaseUrl.Contains(CoinMetaData.BlockHeightPH))
                        blockExplorerLink = blockInfobaseUrl.Replace(CoinMetaData.BlockHeightPH, block.BlockHeight.ToString(CultureInfo.InvariantCulture));
                    else if(blockInfobaseUrl.Contains(CoinMetaData.BlockHashPH) && !string.IsNullOrEmpty(block.Hash))
                        blockExplorerLink = blockInfobaseUrl.Replace(CoinMetaData.BlockHashPH, block.Hash);
                }

                // miner account explorer link
                if(!string.IsNullOrEmpty(coin.ExplorerAccountLink))
                    minerExplorerLink = string.Format(coin.ExplorerAccountLink, block.Miner);
            }

            messageBus.SendMessage(new BlockUnlockedNotification
            {
                PoolId = poolId,
                BlockHeight = block.BlockHeight,
                BlockType = block.Type,
                Symbol = coin.Symbol,
                Reward = block.Reward,
                Status = block.Status,
                Effort = block.Effort,
                BlockHash = block.Hash,
                ExplorerLink = blockExplorerLink,
                Miner = block.Miner,
                MinerExplorerLink = minerExplorerLink,
            });
        }

        public static void NotifyChainHeight(this IMessageBus messageBus, string poolId, ulong height, CoinTemplate coin)
        {
            messageBus.SendMessage(new NewChainHeightNotification
            {
                PoolId = poolId,
                BlockHeight = height,
                Symbol = coin.Symbol,
            });
        }

        public static void NotifyHashrateUpdated(this IMessageBus messageBus, string poolId, double hashrate, string miner = null, string worker = null)
        {
            messageBus.SendMessage(new HashrateNotification
            {
                PoolId = poolId,
                Hashrate = hashrate,
                Miner = miner,
                Worker = worker,
            });
        }
    }
}
