using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Networking
{
	public static class IpAddressUtils
	{
		public static IPAddress GetLocal()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			return host.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
		}
	}
}