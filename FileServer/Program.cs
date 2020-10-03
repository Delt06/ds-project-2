using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
		private static readonly object Mutex = new object();
		private static INode? _lastTree;
		private static string _pathPrefix = string.Empty;

		private static void Main(string[] args)
		{
			Console.Write("Input port: ");
			var port = int.Parse(Console.ReadLine() ?? string.Empty);
			_pathPrefix = $"fs{port}_";

			HandleBackup(port);

			Console.Write("Input backup port: ");
			var backupPort = int.Parse(Console.ReadLine() ?? string.Empty);
			LaunchBackupThread(backupPort);

			var ip = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(ip, port);
			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(local);
			socket.Listen(1);
			using var client = socket.Accept();

			var buffer = new byte[32000];

			while (true)
			{
				var command = client.ReceiveUntilEof(buffer).To<ICommand>();
				Console.WriteLine($"Received {command}.");

				lock (Mutex)
				{
					if (command is StatefulCommand statefulCommand)
						_lastTree = statefulCommand.Root;

					if (_lastTree == null) continue;

					Console.WriteLine(_lastTree);

					var visitor = new CommandHandleVisitor(_lastTree);
					command.Accept(visitor);
					Console.WriteLine($"Handled {command}.");

					ICommand response = visitor.Payload != null
						? new PayloadResponseCommand(command, visitor.Payload, visitor.PayloadPath, _lastTree)
						: new ResponseCommand(command);
					client.SendCompletelyWithEof(response.ToBytes());
				}

				Console.WriteLine("Sent response.");
			}
		}

		private static void HandleBackup(int port)
		{
			Console.Write("Load from another server (y/n)? ");

			while (true)
			{
				var input = Console.ReadLine()?.Trim()?.ToLower() ?? string.Empty;
				if (input.Equals("n")) return;
				if (input.Equals("y")) break;

				Console.WriteLine("Invalid input.");
			}

			Console.Write("Input backup server port: ");
			var remotePort = int.Parse(Console.ReadLine() ?? string.Empty);
			var address = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(address, port);
			var remote = new IPEndPoint(address, remotePort);

			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(local);
			socket.Connect(remote);


			var buffer = new byte[256000];
			socket.SendCompletelyWithEof(Array.Empty<byte>().AsSpan());
			var backupData = socket.ReceiveUntilEof(buffer).To<BackupData>();

			lock (Mutex)
			{
				_lastTree = backupData.Tree;
				if (_lastTree == null) return;

				if (TryPrefixedGetPathTo(_lastTree, _lastTree.Id, out var rootPath, out _))
					if (Directory.Exists(rootPath))
						Directory.Delete(rootPath, true);

				foreach (var (id, data) in backupData.Files)
				{
					if (!TryPrefixedGetPathTo(_lastTree, id, out var path, out _)) continue;

					var directory = Path.GetDirectoryName(path);
					if (!Directory.Exists(directory))
						Directory.CreateDirectory(directory);

					using var file = File.Create(path);
					file.Write(data);
					Console.WriteLine($"Loaded file with ID {id} of size {data.Length}.");
				}
			}

			Console.WriteLine("Backup finished.");
		}

		private static void LaunchBackupThread(int port)
		{
			new Thread(() =>
			{
				var address = IpAddressUtils.GetLocal();
				var local = new IPEndPoint(address, port);
				using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
				socket.Bind(local);
				socket.Listen(1);

				while (true)
				{
					try
					{
						using var client = socket.Accept();
						client.ReceiveUntilEof(new byte[32]);

						lock (Mutex)
						{
							var tree = _lastTree?.Clone();
							var backupData = new BackupData(tree);

							if (tree != null)
							{
								var files = GetFiles(tree);
								backupData.Files.AddRange(files);
							}

							client.SendCompletelyWithEof(backupData.ToBytes());
							Console.WriteLine($"Send backup data of {backupData.Files.Count} files.");
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
			}).Start();
		}

		private static IEnumerable<(int id, byte[] data)> GetFiles(INode root)
		{
			foreach (var node in Traverse(root))
			{
				if (!TryPrefixedGetPathTo(root, node.Id, out var path, out _)) continue;

				if (File.Exists(path))
					yield return (node.Id, File.ReadAllBytes(path));
			}
		}

		private static IEnumerable<INode> Traverse(INode root)
		{
			yield return root;

			foreach (var child in root.Children)
			{
				foreach (var node in Traverse(child))
				{
					yield return node;
				}
			}
		}

		private static bool TryPrefixedGetPathTo(INode root, int nodeId, out string path, out string normalPath)
		{
			if (!TryGetPathTo(root, nodeId, out normalPath))
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

		private class CommandHandleVisitor : ICommandVisitor
		{
			public byte[]? Payload { get; private set; }
			public string PayloadPath { get; private set; } = string.Empty;

			private readonly INode _root;

			public CommandHandleVisitor(INode root) => _root = root;

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

			public void Visit(FileInfoCommand command) { }

			public void Visit(FileCopyCommand command)
			{
				if (!TryPrefixedGetPathTo(command.FileId, out var path)) return;

				var directory = Path.GetDirectoryName(path) ?? string.Empty;
				var fileData = File.ReadAllBytes(path);
				var copyPath = Path.Combine(directory, command.CopyName);

				if (!File.Exists(copyPath))
					File.WriteAllBytes(copyPath, fileData);
			}

			public void Visit(FileMoveCommand command)
			{
				if (command.PreMoveTree == null) return;
				if (!TryPrefixedGetPathTo(command.PreMoveTree, command.FileId, out var filePath)) return;
				if (!TryPrefixedGetPathTo(command.PreMoveTree, command.DestinationDirectoryId,
					out var destinationDirectoryPath)) return;

				var fileName = Path.GetFileName(filePath);
				var destinationFilePath = Path.Combine(destinationDirectoryPath, fileName);
				File.Move(filePath, destinationFilePath);
			}

			private bool TryPrefixedGetPathTo(int nodeId, out string path) =>
				TryPrefixedGetPathTo(nodeId, out path, out _);

			private bool TryPrefixedGetPathTo(int nodeId, out string path, out string normalPath) =>
				Program.TryPrefixedGetPathTo(_root, nodeId, out path, out normalPath);

			private bool TryPrefixedGetPathTo(INode root, int nodeId, out string path) =>
				Program.TryPrefixedGetPathTo(root, nodeId, out path, out _);
		}
	}
}