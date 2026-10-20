using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.GetMyIp
{
    public class AmazonAwsCheckIpService : IGetMyIpService
    {
        private const string serviceUrl = "https://checkip.amazonaws.com";

        public AmazonAwsCheckIpService()
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
            return false;
        }

        public async Task<IPAddress> GetIpv6Async()
        {
            throw new NotImplementedException();
        }
    }
}