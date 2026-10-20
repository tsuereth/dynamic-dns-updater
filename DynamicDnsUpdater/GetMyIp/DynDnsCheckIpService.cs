using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.GetMyIp
{
    public class DynDnsCheckIpService : IGetMyIpService
    {
        private const string serviceUrl = "https://checkip.dyndns.org";

        private static readonly Regex htmlResponsePattern = new Regex(@"<body>Current IP Address: ([0-9\.]+)</body>", RegexOptions.Compiled);

        public DynDnsCheckIpService()
        {
        }

        private static IPAddress ParseServiceResponse(AddressFamily expectedAddressFamily, string serviceResponse)
        {
            var responseMatch = htmlResponsePattern.Match(serviceResponse);
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
            return false;
        }

        public async Task<IPAddress> GetIpv6Async()
        {
            throw new NotImplementedException();
        }
    }
}