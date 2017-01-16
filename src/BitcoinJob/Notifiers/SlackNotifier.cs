﻿using System;
using System.Threading.Tasks;
using AzureStorage.Queue;
using Core;
using Newtonsoft.Json;

namespace BackgroundWorker.Notifiers
{
	public interface ISlackNotifier
	{
	    Task WarningAsync(string message);
	}

	public class SlackNotifier : ISlackNotifier
    {
		private readonly IQueueExt _queue;

		public SlackNotifier(Func<string, IQueueExt> queueFactory)
		{
			_queue = queueFactory(Constants.SlackNotifierQueue);
		}

		public async Task WarningAsync(string message)
		{
			var obj = new
			{
                Type = "Warnings",
                Sender = "bitcoin service",
                Message = message
			};
            
			await _queue.PutRawMessageAsync(JsonConvert.SerializeObject(obj));
		}
	}
}