using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DynamicDnsUpdater.GetMyIp
{
    public static class HttpServiceUtils
    {
        public static async Task<string> GetResponseStringAsync(AddressFamily addressFamily, Uri serviceUri)
        {
            // NOTE: This implementation does not share, or even cache, HttpClient instances.
            // It assumes a use-case where the client is used for a single request, then discarded.
            var addressFamilySocketHandler = new SocketsHttpHandler()
            {
                ConnectCallback = async (context, cancellationToken) =>
                {
                    var dnsEntry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, addressFamily, cancellationToken);

                    var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.NoDelay = true;

                    try
                    {
                        await socket.ConnectAsync(dnsEntry.AddressList, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }
            };

            using (var httpClient = new HttpClient(addressFamilySocketHandler))
            using (var request = new HttpRequestMessage(HttpMethod.Get, serviceUri))
            {
                var response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }

        public static IPAddress ParseAddressString(AddressFamily expectedAddressFamily, string addressString)
        {
            if (!IPAddress.TryParse(addressString.Trim(), out var address))
            {
                throw new InvalidDataException($"Failed to parse IP address from string: {addressString}");
            }

            if (address.AddressFamily != expectedAddressFamily)
            {
                throw new InvalidDataException($"Parsed unexpected IP address family (wanted {expectedAddressFamily}, got {address.AddressFamily}) from string: {addressString}");
            }

            return address;
        }
    }
}