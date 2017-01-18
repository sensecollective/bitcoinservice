﻿using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BitcoinApi.Filters;
using BitcoinApi.Models;
using Common;
using Common.Log;
using Core.Exceptions;
using Core.OpenAssets;
using Core.Repositories.Assets;
using Core.TransactionQueueWriter;
using Core.TransactionQueueWriter.Commands;
using LkeServices.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace BitcoinApi.Controllers
{
    [Route("api/[controller]")]
    public class EnqueueTransactionController : Controller
    {
        private readonly ILykkeTransactionBuilderService _builder;
        private readonly IAssetRepository _assetRepository;        
        private readonly ILog _log;
        private readonly ITransactionQueueWriter _transactionQueueWriter;        

        public EnqueueTransactionController(ILykkeTransactionBuilderService builder,
            IAssetRepository assetRepository,            
            ILog log,            
            ITransactionQueueWriter transactionQueueWriter)
        {
            _builder = builder;
            _assetRepository = assetRepository;            
            _log = log;
            _transactionQueueWriter = transactionQueueWriter;            
        }

        /// <summary>
        /// Add transfer transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateCashout([FromBody]TransferRequest model)
        {
            await Log("Transfer", "Begin", model);

            var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.SourceAddress);
            if (sourceAddress == null)
                throw new BackendException("Invalid source address provided", ErrorCode.InvalidAddress);

            var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.DestinationAddress);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Transfer, new TransferCommand
            {
                Amount = model.Amount,
                SourceAddress = model.SourceAddress,
                Asset = model.Asset,
                DestinationAddress = model.DestinationAddress
            }.ToJson());

            await Log("Transfer", "End", model, transactionId);

            return Ok(new TransactionResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add transfer all transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("transferall")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateTransferAll([FromBody]TransferAllRequest model)
        {
            await Log("TransferAll", "Begin", model);

            var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.SourceAddress);
            if (sourceAddress == null)
                throw new BackendException("Invalid source address provided", ErrorCode.InvalidAddress);

            var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.DestinationAddress);
            if (destAddress == null)
                throw new BackendException("Invalid destination address provided", ErrorCode.InvalidAddress);
            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.TransferAll, new TransferAllCommand
            {
                SourceAddress = model.SourceAddress,
                DestinationAddress = model.DestinationAddress,
            }.ToJson());


            await Log("TransferAll", "End", model, transactionId);
            return Ok(new TransactionResponse
            {
                TransactionId = transactionId
            });
        }



        /// <summary>
        /// Add swap transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("swap")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> CreateSwap([FromBody]SwapRequest model)
        {
            await Log("Swap", "Begin", model);

            var bitcoinAddres1 = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.MultisigCustomer1);
            if (bitcoinAddres1 == null)
                throw new BackendException("Invalid MultisigCustomer1 provided", ErrorCode.InvalidAddress);

            var bitcoinAddres2 = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.MultisigCustomer2);
            if (bitcoinAddres2 == null)
                throw new BackendException("Invalid MultisigCustomer2 provided", ErrorCode.InvalidAddress);

            var asset1 = await _assetRepository.GetAssetById(model.Asset1);
            if (asset1 == null)
                throw new BackendException("Provided Asset1 is missing in database", ErrorCode.AssetNotFound);

            var asset2 = await _assetRepository.GetAssetById(model.Asset2);
            if (asset2 == null)
                throw new BackendException("Provided Asset2 is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Swap, new SwapCommand
            {
                MultisigCustomer1 = model.MultisigCustomer1,
                Amount1 = model.Amount1,
                Asset1 = model.Asset1,
                MultisigCustomer2 = model.MultisigCustomer2,
                Amount2 = model.Amount2,
                Asset2 = model.Asset2
            }.ToJson());

            await Log("Swap", "End", model, transactionId);

            return Ok(new TransactionResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add issue transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("issue")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Issue([FromBody] IssueRequest model)
        {
            await Log("Issue", "Begin", model);

            var bitcoinAddres = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.Address);
            if (bitcoinAddres == null)
                throw new BackendException("Invalid Address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided Asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Issue, new IssueCommand
            {
                Amount = model.Amount,
                Asset = model.Asset,
                Address = model.Address
            }.ToJson());


            await Log("Issue", "End", model, transactionId);

            return Ok(new TransactionResponse
            {
                TransactionId = transactionId
            });
        }

        /// <summary>
        /// Add destroy transaction to queue for building
        /// </summary>
        /// <returns>Internal transaction id</returns>
        [HttpPost("destroy")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(typeof(ApiException), 400)]
        public async Task<IActionResult> Destroy([FromBody] DestroyRequest model)
        {
            await Log("Destroy", "Begin", model);

            var bitcoinAddres = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(model.Address);
            if (bitcoinAddres == null)
                throw new BackendException("Invalid Address provided", ErrorCode.InvalidAddress);

            var asset = await _assetRepository.GetAssetById(model.Asset);
            if (asset == null)
                throw new BackendException("Provided Asset is missing in database", ErrorCode.AssetNotFound);

            var transactionId = await _builder.AddTransactionId(model.TransactionId);

            await _transactionQueueWriter.AddCommand(transactionId, TransactionCommandType.Destroy, new DestroyCommand
            {
                Amount = model.Amount,
                Asset = model.Asset,
                Address = model.Address,
            }.ToJson());

            await Log("Destroy", "End", model, transactionId);

            return Ok(new TransactionResponse
            {
                TransactionId = transactionId
            });
        }

        private async Task Log(string method, string status, object model, Guid? transactionId = null)
        {
            var properties = model.GetType().GetTypeInfo().GetProperties();
            var builder = new StringBuilder();
            foreach (var prop in properties)
                builder.Append($"{prop.Name}: [{prop.GetValue(model)}], ");

            if (transactionId.HasValue)
                builder.Append($"Transaction: [{transactionId}]");

            await _log.WriteInfoAsync("TransactionController", method, status, builder.ToString());
        }

    }
}
