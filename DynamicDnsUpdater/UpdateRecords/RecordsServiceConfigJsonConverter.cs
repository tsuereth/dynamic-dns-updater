using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DynamicDnsUpdater.UpdateRecords
{
    public class RecordsServiceConfigJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(UpdateRecordsConfig).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var untypedObject = JObject.Load(reader);
            string serviceTypeString;
            if (untypedObject.TryGetValue("ServiceType", out var serviceTypeToken))
            {
                serviceTypeString = serviceTypeToken.Value<string>();
            }
            else
            {
                throw new InvalidDataContractException("Failed to extract ServiceType string");
            }

            UpdateRecordsConfig serviceConfig;
            switch (serviceTypeString)
            {
                case CloudflareServiceConfig.CloudflareServiceType:
                    serviceConfig = new CloudflareServiceConfig();
                    break;
                default:
                    throw new InvalidDataContractException($"Unrecognized ServiceType string '{serviceTypeString}'");
            }

            serializer.Populate(untypedObject.CreateReader(), serviceConfig);
            return serviceConfig;
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}