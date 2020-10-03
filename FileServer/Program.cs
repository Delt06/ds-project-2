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

				ICommand response = visitor.Payload.Length > 0
					? new PayloadResponseCommand(command, visitor.Payload, visitor.PayloadPath, _lastTree)
					: new ResponseCommand(command);

				client.SendCompletelyWithEof(response.ToBytes());

				Console.WriteLine("Sent response.");
			}
		}

		private static INode? _lastTree;

		private class CommandHandleVisitor : ICommandVisitor
		{
			public byte[] Payload { get; private set; } = Array.Empty<byte>();
			public string PayloadPath { get; private set; } = string.Empty;

			private readonly INode _root;
			private readonly string _pathPrefix;

			public CommandHandleVisitor(INode root, string pathPrefix)
			{
				_root = root;
				_pathPrefix = pathPrefix;
			}

			public void Visit(ICommand command) { }

			public void Visit(ExitCommand command) { }

			public void Visit(CreateFileCommand command)
			{
				if (!TryPrefixedGetPathTo(command.DirectoryId, out var pathToDirectory)) return;

				var path = Path.Combine(pathToDirectory, command.Name);
				Directory.CreateDirectory(pathToDirectory);

				if (File.Exists(path))
					File.Delete(path);

				using var file = File.Create(path);
			}

			public void Visit(DeleteCommand command)
			{
				if (!TryPrefixedGetPathTo(command.NodeId, out var path)) return;

				if (File.Exists(path))
					File.Delete(path);
				else if (Directory.Exists(path))
					Directory.Delete(path, true);
			}

			public void Visit(MakeDirectoryCommand command)
			{
				if (!TryPrefixedGetPathTo(command.ParentDirectoryId, out var pathToParent)) return;

				var path = Path.Combine(pathToParent, command.Name);
				Directory.CreateDirectory(path);
			}

			public void Visit(UploadFileCommand command)
			{
				if (!TryPrefixedGetPathTo(command.DirectoryId, out var directoryPath)) return;

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
				if (!TryPrefixedGetPathTo(_root.Id, out var path)) return;

				if (Directory.Exists(path))
					Directory.Delete(path, true);
			}

			public void Visit(ReadDirectoryCommand command) { }

			public void Visit(DownloadFileCommand command)
			{
				if (!TryPrefixedGetPathTo(command.Id, out var path, out var normalPath)) return;
				if (!File.Exists(path)) return;

				Payload = File.ReadAllBytes(path);
				PayloadPath = normalPath;
			}

			public void Visit(FileInfoCommand command)
			{
				
			}

			private bool TryPrefixedGetPathTo(int nodeId, out string path) =>
				TryPrefixedGetPathTo(nodeId, out path, out _);

			private bool TryPrefixedGetPathTo(int nodeId, out string path, out string normalPath)
			{
				if (!TryGetPathTo(_root, nodeId, out normalPath))
				{
					path = default!;
					return false;
				}

				path = _pathPrefix + normalPath;
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