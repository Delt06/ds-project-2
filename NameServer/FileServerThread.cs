using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Commands;
using Commands.Serialization;
using Networking;

namespace NameServer
{
	public sealed class FileServerThread
	{
		private readonly int _localPort;
		private readonly EndPoint _remote;
		private readonly int _serverIndex;
		private readonly ConcurrentQueue<ICommand> _commands;
		private readonly ConcurrentQueue<ICommand> _responses;
		
		public FileServerThread(int localPort, EndPoint remote, int serverIndex, ConcurrentQueue<ICommand> responses, ConcurrentQueue<ICommand> commands)
		{
			_localPort = localPort;
			_remote = remote;
			_serverIndex = serverIndex;
			_responses = responses;
			_commands = commands;
		}

		public void Start()
		{
			var buffer = new byte[32000];

			while (true)
			{
				try
				{
					var localAddress = IpAddressUtils.GetLocal();
					var local = new IPEndPoint(localAddress, _localPort);
					using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
					{
						SendTimeout = 500, ReceiveTimeout = 500
					};
					socket.Bind(local);
					Console.WriteLine($"Connecting to file server {_serverIndex + 1}...");
					socket.Connect(_remote);
					Console.WriteLine($"Connected to file server {_serverIndex + 1}.");

					while (true)
					{
						if (!_commands.TryDequeue(out var command))
						{
							continue;
						}

						Console.WriteLine($"Sending command to the file server {_serverIndex + 1}...");
						socket.SendCompletelyWithEof(command.ToBytes());
						Console.WriteLine($"Waiting for a response from the file server {_serverIndex + 1}...");
						var response = socket.ReceiveUntilEof(buffer).To<ICommand>();
						_responses.Enqueue(response);
						Console.WriteLine($"Synchronized with file server {_serverIndex + 1}: {response}");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"Disconnected from file server {_serverIndex + 1}.");
					Console.WriteLine(e);
					Thread.Sleep(500);
				}
			}
		}
	}
}