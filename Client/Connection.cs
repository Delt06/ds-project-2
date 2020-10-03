using System;
using System.Net;
using System.Net.Sockets;
using Commands;
using Commands.Serialization;
using Networking;

namespace Client
{
	public class Connection : IDisposable
	{
		private readonly Socket _socket;
		private readonly byte[] _buffer = new byte[32000];

		public Connection(EndPoint local, EndPoint remote)
		{
			_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			_socket.Bind(local);
			_socket.Connect(remote);
		}

		public void Send(ICommand command)
		{
			_socket.SendCompletely(command.ToBytes());
			_socket.SendCompletely(Conventions.Eof);
		}

		public ICommand Receive()
		{
			return _socket.ReceiveUntilEof(_buffer).To<ICommand>();
		}
		
		public void Dispose()
		{
			if (_disposed) return;

			_socket.Dispose();
			_disposed = true;
		}

		~Connection() => Dispose();

		private bool _disposed;
	}
}