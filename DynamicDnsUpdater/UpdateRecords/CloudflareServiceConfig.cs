using Microsoft.Extensions.Logging;

namespace DynamicDnsUpdater.UpdateRecords
{
    public class CloudflareServiceConfig : UpdateRecordsConfig
    {
        public const string CloudflareServiceType = "Cloudflare";

        public string AccountId { get; set; }

        public string ApiToken { get; set; }

        public override CloudflareService CreateService(ILoggerFactory loggerFactory)
        {
            return new CloudflareService(loggerFactory, this.HostedZone, this.RecordNames, this.AccountId, this.ApiToken);
        }
    }
}