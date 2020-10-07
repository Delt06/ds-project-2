using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Commands;
using Commands.Serialization;
using Files;
using Networking;

namespace NameServer
{
	internal static class Program
	{
		private static readonly ConcurrentQueue<ICommand> Responses = new ConcurrentQueue<ICommand>();
		private static readonly TreeFactory Factory = new TreeFactory();
		private static INode _root = Factory.CreateDirectory("root");
		private static readonly Timestamp Timestamp = new Timestamp();
		private const int Port = Conventions.NameServerPort;
		private const int ResponseQueueTimeout = 5000;

		private static void Main(string[] args)
		{
			var address = IpAddressUtils.GetLocal();
			Console.WriteLine($"Launching on {address}:{Port}...");
			
			var queues = StartFileServerThreads(args);

			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			var endpoint = new IPEndPoint(address, Port);
			socket.Bind(endpoint);
			socket.Listen(1);

			Initialize();

			while (true)
			{
				using var client = socket.Accept();

				Console.WriteLine("A client has connected.");
				HandleClient(client, queues);
				Console.WriteLine("A client has disconnected.");
			}
		}

		private static void Initialize()
		{
			Factory.Reset();
			Timestamp.Reset();
			_root = Factory.CreateDirectory("root");
		}

		private static ImmutableArray<ConcurrentQueue<ICommand>> StartFileServerThreads(string[] args)
		{
			var fileServersCount = args.Length;
			Console.WriteLine($"Configuring {fileServersCount} file servers...");
			var queues = Enumerable.Range(0, fileServersCount)
				.Select(i => new ConcurrentQueue<ICommand>())
				.ToImmutableArray();
			var threads = new FileServerThread[fileServersCount];

			for (var i = 0; i < fileServersCount; i++)
			{
				var serverIndex = i;
				var remote = IPEndPoint.Parse(args[i]);
				var threadPort = Port + 1 + serverIndex;
				threads[i] = new FileServerThread(threadPort, remote, serverIndex, Responses, queues[serverIndex]);
			}

			foreach (var thread in threads)
			{
				thread.Start();
			}

			return queues;
		}

		private static void HandleClient(Socket client, ImmutableArray<ConcurrentQueue<ICommand>> queues)
		{
			try
			{
				var visitor = new ExecuteCommandVisitor(() => _root, Factory, Initialize, Timestamp);
				var buffer = new byte[32000];

				while (!visitor.Exit)
				{
					var data = client.ReceiveUntilEof(buffer);
					var receivedCommand = data.To<ICommand>();

					Console.WriteLine(receivedCommand);
					receivedCommand.Accept(visitor);
					var treeClone = _root.Clone();
					var timestampClone = Timestamp.Clone();
					var statefulCommand = new StatefulCommand(treeClone, receivedCommand, timestampClone);
					
					Responses.Clear();

					foreach (var queue in queues)
					{
						queue.Enqueue(statefulCommand);
					}

					if (visitor.AwaitResponse)
					{
						ICommand response;
						var watch = Stopwatch.StartNew();

						while (true)
						{
							if (!Responses.TryDequeue(out response!))
							{
								if (watch.ElapsedMilliseconds >= ResponseQueueTimeout)
								{
									response = new ResponseCommand(receivedCommand, "Timeout");
									break;
								}
								continue;
							}
							
							watch.Restart();
							Console.WriteLine("Received a response...");
							
							if (!(response is PayloadResponseCommand payloadResponse))
							{
								Console.WriteLine("Response has no payload.");
								continue;
							}

							if (!payloadResponse.Root.Equals(_root))
							{
								Console.WriteLine("Response tree is different.");
								continue;
							}

							if (!payloadResponse.Timestamp.Equals(Timestamp))
							{
								Console.WriteLine("Response timestamp is different.");
								continue;
							}

							break;
						}

						client.SendCompletelyWithEof(response.ToBytes());
					}
					else
					{
						var response = new ResponseCommand(receivedCommand, visitor.Message);
						client.SendCompletelyWithEof(response.ToBytes());
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}
}