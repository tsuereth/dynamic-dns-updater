using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DynamicDnsUpdater.GetMyIp
{
    public class RipeStatWhatsMyIpService : IGetMyIpService
    {
        private const string serviceUrl = "https://stat.ripe.net/data/whats-my-ip/data.json";

        public RipeStatWhatsMyIpService()
        {
        }

        private static IPAddress ParseServiceResponse(AddressFamily expectedAddressFamily, string serviceResponse)
        {
            var responseObject = JObject.Parse(serviceResponse);

            var responseDataIp = responseObject.SelectToken("data.ip");
            if (responseDataIp == null)
            {
                throw new InvalidDataException($"Missing expected 'data.ip' property in service response: {serviceResponse}");
            }

            return HttpServiceUtils.ParseAddressString(expectedAddressFamily, responseDataIp.Value<string>());
        }

        public bool IsIpv4Available()
        {
            return true;
        }

        public async Task<IPAddress> GetIpv4Async()
        {
            var serviceResponse = await HttpServiceUtils.GetResponseStringAsync(AddressFamily.InterNetwork, new Uri(serviceUrl));
            return ParseServiceResponse(AddressFamily.InterNetwork, serviceResponse);
        }

        public bool IsIpv6Available()
        {
            return true;
        }

        public async Task<IPAddress> GetIpv6Async()
        {
            var serviceResponse = await HttpServiceUtils.GetResponseStringAsync(AddressFamily.InterNetworkV6, new Uri(serviceUrl));
            return ParseServiceResponse(AddressFamily.InterNetworkV6, serviceResponse);
        }
    }
}