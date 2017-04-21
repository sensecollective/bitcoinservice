using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace TransferAssets
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var container = Binder.BindDependencies();

            try
            {
                //container.Resolve<ChangeRobotAddressJob>().Start().Wait();
                container.Resolve<TransferJob>().Start().Wait();
                Console.WriteLine("All queues are updated. Press Enter to exit");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal Exception: " + ex.Message + " " + ex.StackTrace);
                Console.ReadLine();
            }
        }
    }
}
