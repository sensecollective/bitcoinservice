using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureRepositories;
using Common;
using Common.Log;
using Core.Bitcoin;
using Core.Settings;
using LkeServices;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Bitcoin.Tests
{
    [SetUpFixture]
    public class Config
    {
        public static IServiceProvider Services { get; set; }
        public static ILog Logger => Services.GetService<ILog>();

        [OneTimeSetUp]
        public void Initialize()
        {
            var settings = GeneralSettingsReader.ReadGeneralSettingsLocal<BaseSettings>("../settings/bitcoinsettings.json");

            settings.RPCServerIpAddress = "52.233.192.225";
            settings.RPCUsername = "bitcoinrpc";
            settings.RPCPassword = "Hasht";

            settings.QBitNinjaBaseUrl = "http://52.233.192.225";

            var log = new LogToConsole();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(settings);
            builder.RegisterInstance(log).As<ILog>();
            builder.RegisterInstance(new RpcConnectionParams(settings));
            builder.BindAzure(settings, log);
            builder.BindCommonServices();

            Services = new AutofacServiceProvider(builder.Build());
        }
    }
}
