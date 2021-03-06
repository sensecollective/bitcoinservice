﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using RestEase;

namespace Core.Providers
{

    public interface ISignatureApi
    {
        [Get("/api/bitcoin/key")]
        Task<PubKeyResponse> GeneratePubKey();

        [Post("/api/bitcoin/sign")]
        Task<TransactionResponse> SignTransaction([Body] TransactionSignRequest request);

        [Get("/api/IsAlive")]
        Task IsAlive();

        [Post("/api/bitcoin/addkey")]
        Task AddKey([Body] AddKeyRequest request);
    }

    public interface ISignatureApiProvider
    {        
        Task<string> GeneratePubKey();
        
        Task<string> SignTransaction(string transaction, SigHash hashType = SigHash.All, string[] additionalSecrets = null);
        Task AddKey(string privateKey);
    }

    public class TransactionSignRequest
    {
        public TransactionSignRequest(string transaction, byte hashType = (byte) SigHash.All, string[] additionalSecrets = null)
        {
            Transaction = transaction;
            HashType = hashType;
            AdditionalSecrets = additionalSecrets;
        }

        public string Transaction { get; set; }

        public byte HashType { get; set; }

        public string[] AdditionalSecrets { get; set; }
    }

    public class AddKeyRequest
    {
        public string Key { get; set; }
    }

    public class PubKeyResponse
    {
        public string PubKey { get; set; }
    }


    public class TransactionResponse
    {
        public string SignedTransaction { get; set; }
    }


}
