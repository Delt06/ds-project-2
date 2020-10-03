using System;
using System.Net.Sockets;

namespace Networking
{
	public static class SocketExt
	{
		public static ReadOnlySpan<byte> ReceiveUntilEof(this Socket socket, byte[] buffer)
		{
			var totalSize = 0;

			while (true)
			{
				var span = new Span<byte>(buffer, totalSize, buffer.Length - totalSize);
				var receivedSize = socket.Receive(span);
				totalSize += receivedSize;

				if (Conventions.ContainsEof(buffer, totalSize))
					break;
			}

			return new Span<byte>(buffer, 0, totalSize);
		}

		public static void SendCompletely(this Socket socket, ReadOnlySpan<byte> data)
		{
			var bytes = data.ToArray();
			var totalSent = 0;

			while (true)
			{
				var span = new ReadOnlySpan<byte>(bytes, totalSent, data.Length - totalSent);
				var receivedSize = socket.Send(span);
				totalSent += receivedSize;

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