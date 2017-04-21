using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Features.ResolveAnything;
using AzureRepositories;
using Common.Log;
using Core.Bitcoin;
using Core.Settings;
using LkeServices;

namespace TransferAssets
{
    public class Binder
    {
        public static IContainer BindDependencies()
        {
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>("../../settings/globalsettings_prod.json");

            var ioc = new ContainerBuilder();

            var log = new LogToConsole();

            ioc.RegisterInstance(log).As<ILog>();
            ioc.RegisterInstance(settings);
            ioc.RegisterInstance(new RpcConnectionParams(settings));

            ioc.BindCommonServices();
            ioc.BindAzure(settings, log);

            ioc.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

            return ioc.Build();
        }
    }
}
