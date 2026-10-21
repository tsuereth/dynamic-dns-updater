using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DynamicDnsUpdater.GetMyIp
{
    public class TestIpv6Service : IGetMyIpService
    {
        private const string serviceUrlIpv4 = "https://ipv4.test-ipv6.com/ip/";

        private const string serviceUrlIpv6 = "https://ipv6.test-ipv6.com/ip/";

        private static readonly Regex jsResponsePattern = new Regex(@"^callback\((.+)\)$", RegexOptions.Compiled);

        public TestIpv6Service()
        {
        }

        private static IPAddress ParseServiceResponse(AddressFamily expectedAddressFamily, string serviceResponse)
        {
            var responseMatch = jsResponsePattern.Match(serviceResponse);
            if (responseMatch == null)
            {
                throw new InvalidDataException($"Unexpected response format: {serviceResponse}");
            }

            var responseObject = JObject.Parse(responseMatch.Groups[1].Value);

            var responseIp = responseObject.SelectToken("ip");
            if (responseIp == null)
            {
                throw new InvalidDataException($"Missing expected 'ip' property in service response: {serviceResponse}");
            }

            return HttpServiceUtils.ParseAddressString(expectedAddressFamily, responseIp.Value<string>());
        }

        public bool IsIpv4Available()
        {
            return true;
        }

        public async Task<IPAddress> GetIpv4Async()
        {
            var serviceResponse = await HttpServiceUtils.GetResponseStringAsync(AddressFamily.InterNetwork, new Uri(serviceUrlIpv4));
            return ParseServiceResponse(AddressFamily.InterNetwork, serviceResponse);
        }

        public bool IsIpv6Available()
        {
            return true;
        }

        public async Task<IPAddress> GetIpv6Async()
        {
            var serviceResponse = await HttpServiceUtils.GetResponseStringAsync(AddressFamily.InterNetworkV6, new Uri(serviceUrlIpv6));
            return ParseServiceResponse(AddressFamily.InterNetworkV6, serviceResponse);
        }
    }
}