using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core;
using Core.Bitcoin;
using Core.OpenAssets;
using Core.Repositories.ExtraAmounts;
using Core.Repositories.TransactionOutputs;
using Core.Repositories.Transactions;
using NBitcoin;
using NBitcoin.OpenAsset;
using LkeServices.Transactions;
using Common;

namespace TransferAssets
{
    public class TransferJob
    {
        private readonly IBitcoinBroadcastService _bitcoinBroadcastService;
        private readonly IBitcoinOutputsService _bitcoinOutputsService;
        private readonly ITransactionBuildHelper _transactionBuildHelper;
        private readonly IPregeneratedOutputsQueueFactory _pregeneratedOutputsQueueFactory;
        private readonly IExtraAmountRepository _extraAmountRepository;
        private readonly TransactionBuildContextFactory _transactionBuildContextFactory;
        private readonly ITransactionBlobStorage _transactionBlobStorage;
        private readonly Func<string, IQueueExt> _queueFactory;

        private List<string> _ignored = new List<string>()
        {
            "ANEjN5ngJJefc8RjKZHeAc37uHKzpwdsc8".ToLower(),
            "AbjAzsV5s5ANrGK3Kf3qbKQVQYSS3McfUH".ToLower(),
            "AJZos2d6vHxAYWLcu9xFudm1MDxK6FTk7u".ToLower(),
            "AJ7wAWbH8Rd5UEHnMC6EoBP5B1gUa7U3cw".ToLower(),
            "AHG1gqienL4Gj5kcJhqm5U5LFCMtt8Vfy6".ToLower(),
            "AUrMzvHTYJ8oQVECCEAXUU4dBM5PvhkP9Z".ToLower()
        };

        private string address = "38DdqhVuqb36jjmxTbvBkiFPXKA8EmW9Ly";

        private string destination = "3KSZHyXAPLNmxs98CsAij8ksAENRkEa3zs";

        public TransferJob(IBitcoinBroadcastService bitcoinBroadcastService, IBitcoinOutputsService bitcoinOutputsService, ITransactionBuildHelper transactionBuildHelper, IPregeneratedOutputsQueueFactory pregeneratedOutputsQueueFactory, IExtraAmountRepository extraAmountRepository, TransactionBuildContextFactory transactionBuildContextFactory, ITransactionBlobStorage transactionBlobStorage, Func<string, IQueueExt> queueFactory)
        {
            _bitcoinBroadcastService = bitcoinBroadcastService;
            _bitcoinOutputsService = bitcoinOutputsService;
            _transactionBuildHelper = transactionBuildHelper;
            _pregeneratedOutputsQueueFactory = pregeneratedOutputsQueueFactory;
            _extraAmountRepository = extraAmountRepository;
            _transactionBuildContextFactory = transactionBuildContextFactory;
            _transactionBlobStorage = transactionBlobStorage;
            _queueFactory = queueFactory;
        }

        public async Task Start()
        {
            var bitcoinAddress = BitcoinAddress.Create(address);

            var destinationAddress = BitcoinAddress.Create(destination);

            var outputs = await _bitcoinOutputsService.GetUnspentOutputs(address);

            var colored = outputs.OfType<ColoredCoin>();

            var groupped = colored.GroupBy(x => x.AssetId);

            foreach (var asset in groupped)
            {
                var assetId = asset.Key.ToString();
                if (_ignored.Contains(assetId) || _ignored.Contains(asset.Key.GetWif(Network.Main).ToWif().ToLower()))
                    continue;

                var allCoins = asset.ToList();

                do
                {

                    var context = _transactionBuildContextFactory.Create(Network.Main);

                    var coins = allCoins.Take(200).ToList();

                    await context.Build(async () =>
                    {
                        var builder = new TransactionBuilder();

                        builder.SetChange(bitcoinAddress);
                        builder.AddCoins(coins);

                        var sum = new AssetMoney(asset.Key);

                        foreach (var coloredCoin in coins)
                            sum += coloredCoin.Amount;

                        long sended = 0;
                        for (var i = 0; i < 20; i++)
                        {
                            builder.SendAsset(destinationAddress, new AssetMoney(asset.Key, sum.Quantity / 20));
                            sended += sum.Quantity / 20;
                        }

                        if (sum.Quantity > sended)
                            builder.SendAsset(destinationAddress, new AssetMoney(asset.Key, sum.Quantity - sended));

                        await _transactionBuildHelper.AddFee(builder, context);

                        var tx = builder.BuildTransaction(true);

                        var guid = Guid.NewGuid();

                        await _transactionBlobStorage.AddOrReplaceTransaction(guid, TransactionBlobType.Initial, tx.ToHex());

                        await _queueFactory(Constants.ClientSignMonitoringQueue).PutRawMessageAsync(new
                        {
                            TransactionId = guid.ToString(),
                            PutDateTime = DateTime.UtcNow
                        }.ToJson());

                        return "";
                    });

                    allCoins = allCoins.Skip(200).ToList();

                } while (allCoins.Any());
            }
        }
    }
}
