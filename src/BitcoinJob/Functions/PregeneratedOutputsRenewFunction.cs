﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Core.Exceptions;
using Core.Repositories.Assets;
using Core.Repositories.TransactionOutputs;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
    public class PregeneratedOutputsRenewFunction
    {
        private readonly IPregeneratedOutputsQueueFactory _queueFactory;
        private readonly ILog _logger;
        private readonly IAssetRepository _assetRepository;
        private readonly BaseSettings _baseSettings;

        public PregeneratedOutputsRenewFunction(IPregeneratedOutputsQueueFactory queueFactory, ILog logger,
            IAssetRepository assetRepository, BaseSettings baseSettings)
        {
            _queueFactory = queueFactory;
            _logger = logger;
            _assetRepository = assetRepository;
            _baseSettings = baseSettings;
        }

        [TimerTrigger("1.00:00:00")]
        public async Task RenewPool()
        {
            await RenewFee();
            await RenewAssets();
        }

        private async Task RenewFee()
        {
            try
            {
                var queue = _queueFactory.CreateFeeQueue();
                var count = await queue.Count();
                for (int i = 0; i < count; i++)
                {
                    var coin = await queue.DequeueCoin();
                    if (coin == null)
                        return;
                    await queue.EnqueueOutputs(coin);
                }
            }
            catch (BackendException)
            {
                //ignore
            }
            catch (Exception e)
            {
                await _logger.WriteErrorAsync("PregeneratedOutputsRenewFunction", "RenewFee", "", e);
            }
        }

        private async Task RenewAssets()
        {
            var assets = (await _assetRepository.GetBitcoinAssets()).Where(o => !string.IsNullOrEmpty(o.AssetAddress) &&
                                                                                !o.IsDisabled &&
                                                                                o.IssueAllowed).ToList();
            foreach (var asset in assets)
            {
                try
                {
                    var queue = _queueFactory.Create(asset.BlockChainAssetId);
                    var count = await queue.Count();
                    for (int i = 0; i < count; i++)
                    {
                        var coin = await queue.DequeueCoin();
                        if (coin == null)
                            return;
                        await queue.EnqueueOutputs(coin);
                    }
                }
                catch (BackendException)
                {
                    //ignore
                }
                catch (Exception e)
                {
                    await _logger.WriteErrorAsync("PregeneratedOutputsRenewFunction", "RenewAssets", $"Asset {asset.Id}", e);
                }
            }
        }
    }
}
