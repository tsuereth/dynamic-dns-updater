using System.Net;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.UpdateRecords
{
    public interface IRecordsService
    {
        public Task UpdateRecordsAsync(IPAddress ipv4, IPAddress ipv6);
    }
}