using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Commands;
using Commands.Serialization;
using Files;
using NameServer;
using Networking;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace FileServer
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			Console.Write("Input port: ");
			var port = int.Parse(Console.ReadLine() ?? string.Empty);
			var ip = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(ip, port);
			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(local);
			socket.Listen(1);
			using var client = socket.Accept();
			var prefix = $"fs{port}_";

			var buffer = new byte[32000];

			while (true)
			{
				var command = client.ReceiveUntilEof(buffer).To<ICommand>();
				Console.WriteLine($"Received {command}.");

				if (command is StatefulCommand statefulCommand)
					_lastTree = statefulCommand.Root;
				
				if (_lastTree == null) continue;
				
				var visitor = new CommandHandleVisitor(_lastTree, prefix);
				command.Accept(visitor);
				Console.WriteLine($"Handled {command}.");
				
				var response = new ResponseCommand(command);
				client.SendCompletelyWithEof(response.ToBytes());
				
				Console.WriteLine("Sent response.");
			}
		}

		private static INode? _lastTree;

		private class CommandHandleVisitor : ICommandVisitor
		{
			private readonly INode _root;
			private readonly string _pathPrefix;

			public CommandHandleVisitor(INode root, string pathPrefix)
			{
				_root = root;
				_pathPrefix = pathPrefix;
			}

			public void Visit(ICommand command)
			{
				
			}

			public void Visit(ExitCommand command)
			{
				
			}

			public void Visit(CreateFileCommand command)
			{
				if (!TryGetPathTo(command.DirectoryId, out var pathToDirectory)) return;
				
				var path = Path.Combine(pathToDirectory, command.Name);
				Directory.CreateDirectory(pathToDirectory);
				
				if (File.Exists(path))
					File.Delete(path);
				
				using var file = File.Create(path);
			}

			public void Visit(DeleteCommand command)
			{
				if (!TryGetPathTo(command.NodeId, out var path)) return;
				
				if (File.Exists(path))
					File.Delete(path);
				else if (Directory.Exists(path))
					Directory.Delete(path, true);
			}

			public void Visit(MakeDirectoryCommand command)
			{
				if (!TryGetPathTo(command.ParentDirectoryId, out var pathToParent)) return;
				
				var path = Path.Combine(pathToParent, command.Name);
				Directory.CreateDirectory(path);
			}

			public void Visit(UploadFileCommand command)
			{
				if (!TryGetPathTo(command.DirectoryId, out var directoryPath)) return;
				
				Directory.CreateDirectory(directoryPath);
				
				var path = Path.Combine(directoryPath, command.Name);

				if (!File.Exists(path))
				{
					using var file = File.Create(path);
					file.Write(command.Data);
				}
				else
				{
					File.WriteAllBytes(path, command.Data);	
				}
			}

			public void Visit(InitializeCommand command)
			{
				if (!TryGetPathTo(_root.Id, out var path)) return;
				
				if (Directory.Exists(path))
					Directory.Delete(path, true);
			}

			public void Visit(ReadDirectoryCommand command)
			{
				
			}

			private bool TryGetPathTo(int nodeId, out string path)
			{
				if (!TryGetPathTo(_root, nodeId, out path)) return false;

				path = _pathPrefix + path;
				return true;
			}

			private static bool TryGetPathTo(INode root, int nodeId, out string path)
			{
				if (root.Id == nodeId)
				{
					path = root.Name;
					return true;
				}

				foreach (var child in root.Children)
				{
					if (!TryGetPathTo(child, nodeId, out var pathEnd)) continue;
					
					path = Path.Combine(root.Name, pathEnd);
					return true;
				}

				path = default!;
				return false;
			}
		}
	}
}