using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DynamicDnsUpdater.UpdateRecords
{
    public abstract class UpdateRecordsConfig
    {
        public string HostedZone { get; set; }

        public List<string> RecordNames { get; set; }

        public string ServiceType { get; set; }

        public abstract IRecordsService CreateService(ILoggerFactory loggerFactory);
    }
}