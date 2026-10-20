using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Cloudflare.NET;
using Cloudflare.NET.Core;
using Cloudflare.NET.Dns.Models;
using Cloudflare.NET.Zones.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicDnsUpdater.UpdateRecords
{
    public class CloudflareService : IRecordsService, System.IDisposable
    {
        private readonly ILogger logger;
        private readonly string hostedZone;
        private readonly List<string> recordNames;
        private readonly HttpClient underlyingHttpClient;
        private readonly CloudflareApiClient cloudflareClient;
        private bool disposed;

        public CloudflareService(ILoggerFactory loggerFactory, string hostedZone, List<string> recordNames, string accountId, string apiToken)
        {
            this.logger = loggerFactory.CreateLogger<CloudflareService>();

            this.hostedZone = hostedZone;
            this.recordNames = recordNames;

            var cloudflareClientOptions = new CloudflareApiOptions()
            {
                AccountId = accountId,
                ApiToken = apiToken,
            };

            this.underlyingHttpClient = new HttpClient();
            CloudflareHttpClientConfigurator.Configure(this.underlyingHttpClient, cloudflareClientOptions, setAuthorizationHeader: true);

            this.cloudflareClient = new CloudflareApiClient(this.underlyingHttpClient, Options.Create(cloudflareClientOptions), loggerFactory);
        }

        public async Task UpdateRecordsAsync(IPAddress ipv4, IPAddress ipv6)
        {
            // API requests require the hosted zone's opaque ID.
            var listZonesFilters = new ListZonesFilters(Name: this.hostedZone);
            var zoneId = string.Empty;
            await foreach (var zone in this.cloudflareClient.Zones.ListAllZonesAsync(listZonesFilters))
            {
                if (zone.Name.Equals(this.hostedZone, StringComparison.OrdinalIgnoreCase))
                {
                    zoneId = zone.Id;
                    break;
                }
            }
            if (string.IsNullOrEmpty(zoneId))
            {
                throw new ArgumentException($"Failed to find a hosted zone with the name {this.hostedZone}");
            }

            // Build a batch of record updates, as appropriate.
            var recordsToUpdateIpv4 = (ipv4 == null) ? new HashSet<string>() : this.recordNames.ToHashSet();
            var recordsToUpdateIpv6 = (ipv6 == null) ? new HashSet<string>() : this.recordNames.ToHashSet();
            var zoneRecordNameSuffix = $".{this.hostedZone}";
            var recordsPatches = new List<BatchPatchOperation>();
            await foreach (var record in this.cloudflareClient.Dns.ListAllDnsRecordsAsync(zoneId))
            {
                string recordNameShort;
                if (record.Name.Equals(this.hostedZone, StringComparison.OrdinalIgnoreCase))
                {
                    recordNameShort = "@";
                }
                else if (record.Name.EndsWith(zoneRecordNameSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    recordNameShort = record.Name.Substring(0, record.Name.Length - zoneRecordNameSuffix.Length);
                }
                else
                {
                    this.logger.LogWarning($"Ignoring unknown name format in hosted zone {this.hostedZone} {record.Type} record: {record.Name}");
                    continue;
                }

                if (record.Type == DnsRecordType.A && recordsToUpdateIpv4.Contains(recordNameShort))
                {
                    recordsToUpdateIpv4.Remove(recordNameShort);

                    if (!record.Content.Equals(ipv4.ToString(), StringComparison.Ordinal))
                    {
                        this.logger.LogInformation($"Preparing update for hosted zone {this.hostedZone} A record {recordNameShort} with outdated ipv4 {record.Content}");
                        recordsPatches.Add(new BatchPatchOperation(Id: record.Id, Content: ipv4.ToString()));
                    }
                    else
                    {
                        this.logger.LogInformation($"Hosted zone {this.hostedZone} A record {recordNameShort} already matches ipv4 {record.Content}");
                    }
                }
                else if (record.Type == DnsRecordType.AAAA && recordsToUpdateIpv6.Contains(recordNameShort))
                {
                    recordsToUpdateIpv6.Remove(recordNameShort);

                    if (!record.Content.Equals(ipv6.ToString(), StringComparison.Ordinal))
                    {
                        this.logger.LogInformation($"Preparing update for hosted zone {this.hostedZone} AAAA record {recordNameShort} with outdated ipv6 {record.Content}");
                        recordsPatches.Add(new BatchPatchOperation(Id: record.Id, Content: ipv6.ToString()));
                    }
                    else
                    {
                        this.logger.LogInformation($"Hosted zone {this.hostedZone} AAAA record {recordNameShort} already matches ipv6 {record.Content}");
                    }
                }
                else
                {
                    //this.logger.LogDebug($"Ignoring hosted zone {this.hostedZone} {record.Type} record: {recordNameShort}");
                }
            }

            if (recordsToUpdateIpv4.Count > 0 || recordsToUpdateIpv6.Count > 0)
            {
                throw new ArgumentException($"Failed to find some records in the hosted zone {this.hostedZone}: A records [{string.Join(',', recordsToUpdateIpv4)}] ; AAAA records [{string.Join(',', recordsToUpdateIpv6)}]");
            }

            if (recordsPatches.Count > 0)
            {
                var batchRequest = new BatchDnsRecordsRequest(Patches: recordsPatches);
                await this.cloudflareClient.Dns.BatchDnsRecordsAsync(zoneId, batchRequest);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.cloudflareClient.Dispose();
                    this.underlyingHttpClient.Dispose();

                    this.disposed = true;
                }
            }
        }
    }
}