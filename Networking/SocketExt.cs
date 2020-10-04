using System;
using System.Diagnostics;
using System.Net.Sockets;

namespace Networking
{
	public static class SocketExt
	{
		private const int Timeout = 1000;
		
		public static ReadOnlySpan<byte> ReceiveUntilEof(this Socket socket, byte[] buffer)
		{
			var totalSize = 0;
			var watch = Stopwatch.StartNew();

			while (true)
			{
				var span = new Span<byte>(buffer, totalSize, buffer.Length - totalSize);
				var receivedSize = socket.Receive(span);
				totalSize += receivedSize;
				
				if (receivedSize != 0)
					watch.Restart();
				else if (watch.ElapsedMilliseconds >= Timeout)
					throw new TimeoutException("Receive timeout.");

				if (Conventions.ContainsEof(buffer, totalSize))
					break;
			}

			return new Span<byte>(buffer, 0, totalSize);
		}

		public static void SendCompletely(this Socket socket, ReadOnlySpan<byte> data)
		{
			var bytes = data.ToArray();
			var totalSent = 0;
			var watch = Stopwatch.StartNew();

			while (true)
			{
				var span = new ReadOnlySpan<byte>(bytes, totalSent, data.Length - totalSent);
				var sentSize = socket.Send(span);
				totalSent += sentSize;
				
				if (sentSize != 0)
					watch.Restart();
				else if (watch.ElapsedMilliseconds >= Timeout)
					throw new TimeoutException("Send timeout.");

				if (totalSent == bytes.Length)
					break;
			}
		}

		public static void SendCompletelyWithEof(this Socket socket, ReadOnlySpan<byte> data)
		{
			socket.SendCompletely(data);
			socket.SendCompletely(Conventions.Eof);
		}
	}
}