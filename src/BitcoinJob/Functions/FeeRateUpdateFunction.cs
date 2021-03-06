﻿using System;
using System.Threading.Tasks;
using Core.Providers;
using Core.Repositories.FeeRate;
using Core.Settings;
using Lykke.JobTriggers.Triggers.Attributes;

namespace BitcoinJob.Functions
{
    public class FeeRateUpdateFunction
    {
        private readonly IFeeRateRepository _feeRateRepository;
        private readonly IFeeRateApiProvider _feerateApiProvider;
        private readonly BaseSettings _settings;

        public FeeRateUpdateFunction(IFeeRateRepository feeRateRepository, IFeeRateApiProvider feerateApiProvider, BaseSettings settings)
        {
            _feeRateRepository = feeRateRepository;
            _feerateApiProvider = feerateApiProvider;
            _settings = settings;
        }

        [TimerTrigger("1:00:00")]
        public async Task FeeRateUpdate()
        {
            FeeResult feeRate = null;
            try
            {
                feeRate = await _feerateApiProvider.GetFee();
            }
            catch (TaskCanceledException)
            {
                //ignored                
                return;
            }            
            int newFeeRate;
            switch (_settings.FeeType)
            {
                case Core.Enums.FeeType21co.FastestFee:
                    newFeeRate = feeRate.FastestFee;
                    break;
                case Core.Enums.FeeType21co.HalfHourFee:
                    newFeeRate = feeRate.HalfHourFee;
                    break;
                case Core.Enums.FeeType21co.HourFee:
                    newFeeRate = feeRate.HourFee;
                    break;
                default:
                    throw new Exception("unsupported fee type");
            }

            await _feeRateRepository.UpdateFeeRate(newFeeRate);
        }
    }
}
