using System.Collections.Generic;
using System.IO;
using DynamicDnsUpdater.UpdateRecords;
using Newtonsoft.Json;

namespace DynamicDnsUpdater
{
    public class Config
    {
        public bool Ipv4Enabled { get; set; } = true;

        public bool Ipv6Enabled { get; set; } = true;

        public bool AmazonAwsCheckIpEnabled { get; set; } = true;

        public bool DynDnsCheckIpEnabled { get; set; } = true;

        public bool HurricaneElectricCheckIpEnabled { get; set; } = true;

        public bool ICanHazIpEnabled { get; set; } = true;

        public bool InformActionIpEchoEnabled { get; set; } = true;

        public bool RipeStatWhatsMyIpEnabled { get; set; } = true;

        public bool TestIpv6Enabled { get; set; } = true;

        public List<UpdateRecordsConfig> UpdateRecords { get; set; } = new();

        public static Config FromFile(string filePath)
        {
            var jsonSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Converters = new List<JsonConverter>()
                {
                    new RecordsServiceConfigJsonConverter(),
                },
            };

            var fileText = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<Config>(fileText, jsonSettings);
        }
    }
}