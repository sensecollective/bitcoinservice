using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Queue;

namespace TransferAssets
{
    public class ChangeRobotAddressJob
    {
        private readonly IQueueExt _queue;

        private const string OldRobot = "38DdqhVuqb36jjmxTbvBkiFPXKA8EmW9Ly";

        private const string NewRobot = "3KSZHyXAPLNmxs98CsAij8ksAENRkEa3zs";

        public ChangeRobotAddressJob(Func<string, IQueueExt> queueFactory)
        {
            _queue = queueFactory("transaction-commands");
        }

        public async Task Start()
        {
            var count = await _queue.Count();
            for (int i = 0; i < count; i++)
            {
                var msg = await _queue.GetRawMessageAsync();
                if (msg != null)
                {
                    var newVersion = msg.AsString.Replace(OldRobot, NewRobot);
                    await _queue.PutRawMessageAsync(newVersion);
                    await _queue.FinishRawMessageAsync(msg);
                }
            }
        }
    }
}
