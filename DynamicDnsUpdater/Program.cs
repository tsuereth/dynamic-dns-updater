using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DynamicDnsUpdater.GetMyIp;
using DynamicDnsUpdater.UpdateRecords;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace DynamicDnsUpdater
{
    internal sealed class Program
    {
        static async Task<int> Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(c => c.AddSystemdConsole());
            var logger = loggerFactory.CreateLogger<Program>();

            var appAssembly = Assembly.GetExecutingAssembly();
            var appVersion = appAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            var printHelp = false;
            var printVersion = false;

            var configFile = "config.json";

            var options = new OptionSet()
            {
                { "help", "Print help text", _ => printHelp = true },
                { "version", "Print application version", _ => printVersion = true },

                { "c|config-file=", $"Config file path, default: {configFile}", o => configFile = o },
            };
            var unexpectedArgs = options.Parse(args);
            if (unexpectedArgs.Count > 0)
            {
                var unexpectedArgsString = string.Join(' ', unexpectedArgs);
                throw new ArgumentException($"Unexpected arguments: {unexpectedArgsString}");
            }

            if (printHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                return 0;
            }
            if (printVersion)
            {
                logger.LogInformation($"{appAssembly.GetName().Name} version {appVersion}");
                return 0;
            }

            logger.LogInformation($"Reading config from: {configFile}");
            Config config;
            try
            {
                config = Config.FromFile(configFile);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to read config from {configFile}");
                return 1;
            }

            var getMyIpServices = new List<IGetMyIpService>();
            if (config.AmazonAwsCheckIpEnabled)
            {
                getMyIpServices.Add(new AmazonAwsCheckIpService());
            }
            if (config.DynDnsCheckIpEnabled)
            {
                getMyIpServices.Add(new DynDnsCheckIpService());
            }
            if (config.HurricaneElectricCheckIpEnabled)
            {
                getMyIpServices.Add(new HurricaneElectricCheckIpService());
            }
            if (config.ICanHazIpEnabled)
            {
                getMyIpServices.Add(new ICanHazIpService());
            }
            if (config.InformActionIpEchoEnabled)
            {
                getMyIpServices.Add(new InformActionIpEchoService());
            }
            if (config.RipeStatWhatsMyIpEnabled)
            {
                getMyIpServices.Add(new RipeStatWhatsMyIpService());
            }
            if (config.TestIpv6Enabled)
            {
                getMyIpServices.Add(new TestIpv6Service());
            }

            if (getMyIpServices.Count == 0)
            {
                throw new ArgumentException("No get-my-ip services are enabled!");
            }

            var recordsServices = new List<IRecordsService>();
            foreach (var recordsConfig in config.UpdateRecords)
            {
                recordsServices.Add(recordsConfig.CreateService(loggerFactory));
            }

            if (recordsServices.Count == 0)
            {
                throw new ArgumentException("No update-records services are configured!");
            }

            IPAddress myIpv4 = null;
            IPAddress myIpv6 = null;

            if (config.Ipv4Enabled)
            {
                var servicesShuffled = getMyIpServices.Where(s => s.IsIpv4Available()).Shuffle();
                var services = new Queue<IGetMyIpService>(servicesShuffled);

                while (myIpv4 == null && services.Count > 0)
                {
                    var service = services.Dequeue();
                    var timer = Stopwatch.StartNew();
                    try
                    {
                        var serviceIpv4 = await service.GetIpv4Async();
                        logger.LogInformation($"Retrieved ipv4 from {service.GetType().Name} after {timer.ElapsedMilliseconds} ms: {serviceIpv4}");

                        myIpv4 = serviceIpv4;
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to retrieve ipv4 from {service.GetType().Name} after {timer.ElapsedMilliseconds} ms");
                    }
                }

                if (myIpv4 == null)
                {
                    logger.LogError("Unable to retrieve an ipv4 address");
                }
            }

            if (config.Ipv6Enabled)
            {
                var servicesShuffled = getMyIpServices.Where(s => s.IsIpv6Available()).Shuffle();
                var services = new Queue<IGetMyIpService>(servicesShuffled);

                while (myIpv6 == null && services.Count > 0)
                {
                    var service = services.Dequeue();
                    var timer = Stopwatch.StartNew();
                    try
                    {
                        var serviceIpv6 = await service.GetIpv6Async();
                        logger.LogInformation($"Retrieved ipv6 from {service.GetType().Name} after {timer.ElapsedMilliseconds} ms: {serviceIpv6}");

                        myIpv6 = serviceIpv6;
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to retrieve ipv6 from {service.GetType().Name} after {timer.ElapsedMilliseconds} ms");
                    }
                }

                if (myIpv6 == null)
                {
                    logger.LogError("Unable to retrieve an ipv6 address");
                }
            }

            if (myIpv4 == null && myIpv6 == null)
            {
                logger.LogError("No IP addresses were retrieved, nothing will be updated");
                return 2;
            }

            foreach (var recordsService in recordsServices)
            {
                var timer = Stopwatch.StartNew();
                try
                {
                    await recordsService.UpdateRecordsAsync(myIpv4, myIpv6);
                    logger.LogInformation($"Updated records in {recordsService.GetType().Name} after {timer.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to update records in {recordsService.GetType().Name} after {timer.ElapsedMilliseconds} ms");
                }
            }

            return 0;
        }
    }
}
