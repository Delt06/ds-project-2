using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
		private const int Port = Conventions.NameServerPort;

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
			_root = Factory.CreateDirectory("root");
		}

		private static ImmutableArray<ConcurrentQueue<ICommand>> StartFileServerThreads(string[] args)
		{
			var fileServersCount = args.Length;
			Console.WriteLine($"Configuring {fileServersCount} file servers...");
			var queues = Enumerable.Range(0, fileServersCount)
				.Select(i => new ConcurrentQueue<ICommand>())
				.ToImmutableArray();
			var threads = new Thread[fileServersCount];

			for (var i = 0; i < fileServersCount; i++)
			{
				var serverIndex = i;
				var remote = IPEndPoint.Parse(args[i]);

				threads[i] = new Thread(arg =>
				{
					var buffer = new byte[32000];

					while (true)
					{
						try
						{
							var localAddress = IpAddressUtils.GetLocal();
							var local = new IPEndPoint(localAddress, Port + 1 + serverIndex);
							using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
							{
								SendTimeout = 500, ReceiveTimeout = 500
							};
							socket.Bind(local);
							Console.WriteLine($"Connecting to file server {serverIndex + 1}...");
							socket.Connect(remote);
							Console.WriteLine($"Connected to file server {serverIndex + 1}.");

							var commands = queues[serverIndex];

							while (true)
							{
								if (!commands.TryDequeue(out var command))
								{
									continue;
								}

								Console.WriteLine($"Sending command to the file server {serverIndex + 1}...");
								socket.SendCompletelyWithEof(command.ToBytes());
								Console.WriteLine($"Waiting for a response from the file server {serverIndex + 1}...");
								var response = socket.ReceiveUntilEof(buffer).To<ICommand>();
								Responses.Enqueue(response);
								Console.WriteLine($"Synchronized with file server {serverIndex + 1}: {response}");
							}
						}
						catch (Exception e)
						{
							Console.WriteLine($"Disconnected from file server {serverIndex + 1}.");
							Console.WriteLine(e);
							Thread.Sleep(500);
						}
					}
				});
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
				var visitor = new ExecuteCommandVisitor();
				var buffer = new byte[32000];

				while (!visitor.Exit)
				{
					var data = client.ReceiveUntilEof(buffer);
					var receivedCommand = data.To<ICommand>();

					Console.WriteLine(receivedCommand);
					receivedCommand.Accept(visitor);
					var treeClone = _root.Clone();
					var statefulCommand = new StatefulCommand(treeClone, receivedCommand);

					foreach (var queue in queues)
					{
						queue.Enqueue(statefulCommand);
					}

					if (visitor.AwaitResponse)
					{
						ICommand response;

						while (true)
						{
							if (!Responses.TryDequeue(out response!)) continue;

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

		private class ExecuteCommandVisitor : ICommandVisitor
		{
			public bool Exit { get; private set; }
			public string? Message { get; private set; }
			public bool AwaitResponse { get; private set; }

			public void Visit(ICommand command)
			{
				Exit = false;
				Message = null;
				AwaitResponse = false;
			}

			public void Visit(ExitCommand command)
			{
				Exit = true;
				Message = null;
			}

			public void Visit(CreateFileCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.DirectoryId, out var node) &&
				    node is Directory directory)
				{
					var existingFile = directory.Children.FirstOrDefault(c => c.Name == command.Name);
					if (existingFile == null)
					{
						var file = Factory.CreateFile(command.Name, 0);
						directory.Children.Add(file);
						Message = $"ID={file.Id}";
					}
					else
					{
						OnNodeAlreadyExists(command.Name, command.DirectoryId);
					}
				}
				else
				{
					OnDirectoryDoesNotExist(command.DirectoryId);
				}
			}

			private void OnNodeAlreadyExists(string name, int directoryId)
			{
				Message = $"Node {name} already exists in the directory {directoryId}.";
			}

			private void OnDirectoryDoesNotExist(int directoryId)
			{
				Message = $"Directory with ID {directoryId} does not exist.";
			}

			public void Visit(DeleteCommand command)
			{
				Visit((ICommand) command);

				if (command.NodeId == 0)
				{
					Message = "Root cannot be deleted.";
					return;
				}

				if (_root.TryFindNode(command.NodeId, out var node) &&
				    _root.TryFindParent(command.NodeId, out var parent) &&
				    parent is Directory parentDirectory)
					parentDirectory.Children.Remove(node);
				else
					Message = $"Node with {command.NodeId} either does not exist or has no parent.";
			}

			public void Visit(MakeDirectoryCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.ParentDirectoryId, out var parent) &&
				    parent is Directory parentDirectory)
				{
					var existingDirectory = parentDirectory.Children.FirstOrDefault(c => c.Name == command.Name);
					if (existingDirectory == null)
					{
						var directory = Factory.CreateDirectory(command.Name);
						parentDirectory.Children.Add(directory);
						Message = $"ID={directory.Id}";
					}
					else
					{
						OnNodeAlreadyExists(command.Name, command.ParentDirectoryId);
					}
				}
				else
				{
					OnDirectoryDoesNotExist(command.ParentDirectoryId);
				}
			}

			public void Visit(UploadFileCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.DirectoryId, out var parent) &&
				    parent is Directory parentDirectory)
				{
					var existingFile = parentDirectory.Children.FirstOrDefault(c => c.Name == command.Name);
					if (existingFile == null)
					{
						var file = Factory.CreateFile(command.Name, command.Data.Length);
						parentDirectory.Children.Add(file);
						Message = $"ID={file.Id}";
					}
					else
					{
						OnNodeAlreadyExists(command.Name, command.DirectoryId);
					}
				}
				else
				{
					OnDirectoryDoesNotExist(command.DirectoryId);
				}
			}

			public void Visit(InitializeCommand command)
			{
				Visit((ICommand) command);
				Initialize();
			}

			public void Visit(ReadDirectoryCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.Id, out var node) &&
				    node is Directory directory)
				{
					var formattedChildren = directory.Children
						.Select(c => $"({c.Name} ID={c.Id})");
					Message = string.Join(",", formattedChildren);
				}
				else
				{
					OnDirectoryDoesNotExist(command.Id);
				}
			}

			public void Visit(DownloadFileCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.Id, out var node) &&
				    node is File)
					AwaitResponse = true;
				else
					OnFileDoesNotExist(command.Id);
			}

			public void Visit(FileInfoCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.Id, out var node) &&
				    node is File file)
					Message = $"ID={file.Id} Size={file.Size}";
				else
					OnFileDoesNotExist(command.Id);
			}

			public void Visit(FileCopyCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindParent(command.FileId, out var parent) &&
				    parent is Directory parentDirectory &&
				    _root.TryFindNode(command.FileId, out var node) &&
				    node is File file)
				{
					var existingFile = parentDirectory.Children.FirstOrDefault(c => c.Name == command.CopyName);

					if (existingFile != null)
					{
						OnNodeAlreadyExists(command.CopyName, parentDirectory.Id);
					}
					else
					{
						var copy = Factory.CreateFile(command.CopyName, file.Size);
						parentDirectory.Children.Add(copy);
						Message = $"ID={copy.Id}";
					}
				}
				else
				{
					Message = $"Node with {command.FileId} either does not exist or has no parent.";
				}
			}

			public void Visit(FileMoveCommand command)
			{
				Visit((ICommand) command);
				command.PreMoveTree = _root.Clone();

				if (_root.TryFindNode(command.FileId, out var file) &&
				    file is File &&
				    _root.TryFindParent(command.FileId, out var directoryNode) &&
				    directoryNode is Directory directory)
				{
					if (_root.TryFindNode(command.DestinationDirectoryId, out var destinationDirectoryNode) &&
					    destinationDirectoryNode is Directory destinationDirectory)
					{
						directory.Children.Remove(file);
						destinationDirectory.Children.Add(file);
					}
					else
					{
						OnDirectoryDoesNotExist(command.DestinationDirectoryId);
					}
				}
				else
				{
					Message = $"Node with {command.FileId} either does not exist or has no parent.";
				}
			}

			private void OnFileDoesNotExist(int fileId)
			{
				Message = $"File with ID {fileId} does not exist.";
			}
		}
	}
}