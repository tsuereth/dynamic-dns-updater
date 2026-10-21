using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.GetMyIp
{
    public class ICanHazIpService : IGetMyIpService
    {
        private const string serviceUrl = "https://icanhazip.com";

        public ICanHazIpService()
        {
        }

        public bool IsIpv4Available()
        {
            return true;
        }

        public async Task<IPAddress> GetIpv4Async()
        {
            var serviceResponse = await HttpServiceUtils.GetResponseStringAsync(AddressFamily.InterNetwork, new Uri(serviceUrl));
            return HttpServiceUtils.ParseAddressString(AddressFamily.InterNetwork, serviceResponse);
        }

        public bool IsIpv6Available()
        {
            return true;
        }

        public async Task<IPAddress> GetIpv6Async()
        {
            var serviceResponse = await HttpServiceUtils.GetResponseStringAsync(AddressFamily.InterNetworkV6, new Uri(serviceUrl));
            return HttpServiceUtils.ParseAddressString(AddressFamily.InterNetworkV6, serviceResponse);
        }
    }
}