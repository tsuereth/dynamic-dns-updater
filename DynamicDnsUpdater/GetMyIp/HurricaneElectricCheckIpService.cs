using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.GetMyIp
{
    public class HurricaneElectricCheckIpService : IGetMyIpService
    {
        private const string serviceUrl = "https://checkip.dns.he.net";

        private static readonly Regex htmlResponsePatternIpv4 = new Regex(@"<body>\s*Your IP address is\s*:\s*([0-9\.]+)\s*</body>", RegexOptions.Compiled);

        private static readonly Regex htmlResponsePatternIpv6 = new Regex(@"<body>\s*Your IP address is\s*:\s*([0-9a-f\:]+)\s*</body>", RegexOptions.Compiled);

        public HurricaneElectricCheckIpService()
        {
        }

        private static IPAddress ParseServiceResponse(AddressFamily expectedAddressFamily, string serviceResponse)
        {
            Match responseMatch;
            switch (expectedAddressFamily)
            {
                case AddressFamily.InterNetwork:
                    responseMatch = htmlResponsePatternIpv4.Match(serviceResponse);
                    break;
                case AddressFamily.InterNetworkV6:
                    responseMatch = htmlResponsePatternIpv6.Match(serviceResponse);
                    break;
                default:
                    throw new ArgumentException($"Unhandled address family {expectedAddressFamily}");
            }
            if (responseMatch == null)
            {
                throw new InvalidDataException($"Unexpected response format: {serviceResponse}");
            }

            return HttpServiceUtils.ParseAddressString(expectedAddressFamily, responseMatch.Groups[1].Value);
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