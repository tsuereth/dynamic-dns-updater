using System.Net;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.GetMyIp
{
    public interface IGetMyIpService
    {
        public bool IsIpv4Available();

        public Task<IPAddress> GetIpv4Async();

        public bool IsIpv6Available();

        public Task<IPAddress> GetIpv6Async();
    }
}