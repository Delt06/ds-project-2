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
		private static FilePathBuilder _pathBuilder = new FilePathBuilder(string.Empty);

		private static void Main(string[] args)
		{
			HandleFallbackArgs(ref args);

			var (localPort, backupPort) = ParseLocalPorts(args);
			
			lock (Mutex)
			{
				_pathBuilder = new FilePathBuilder($"fs{localPort}_");	
			}

			if (TryParseRemoteBackupAddress(args, out var remoteBackupIp, out var remoteBackupPort))
			{
				HandleBackup(localPort, remoteBackupIp, remoteBackupPort);
			}

			lock (Mutex)
			{
				new BackupWorker(Mutex, () => _lastTree, _pathBuilder).LaunchOn(backupPort);	
			}

			var ip = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(ip, localPort);
			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			socket.Bind(local);
			
			Console.WriteLine($"File server is listening on {local}.");
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

					var visitor = new CommandHandleVisitor(_lastTree, _pathBuilder);
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

		private static void HandleFallbackArgs(ref string[] args)
		{
			if (args.Length == 0)
			{
				args = new[]
				{
					Console.ReadLine() ?? string.Empty,
					Console.ReadLine() ?? string.Empty,
					Console.ReadLine() ?? string.Empty,
				};
			}
		}

		private static (int localPort, int localBackupPort) ParseLocalPorts(string[] args)
		{
			var localPort = int.Parse(args[0]);
			var localBackupPort = int.Parse(args[1]);
			return (localPort, localBackupPort);
		}

		private static bool TryParseRemoteBackupAddress(string[] args, out IPAddress address, out int port)
		{
			if (args.Length == 3)
			{
				var parts = args[2].Split(':', 2);
				address = IPAddress.Parse(parts[0]);
				port = int.Parse(parts[1]);
				return true;
			}

			address = IPAddress.None;
			port = default;
			return false;
		}

		private static void HandleBackup(int localPort, IPAddress remoteBackupIp, int remoteBackupPort)
		{
			var localIp = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(localIp, localPort);
			var remote = new IPEndPoint(remoteBackupIp, remoteBackupPort);

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

				if (_pathBuilder.TryGetPrefixedPath(_lastTree, _lastTree.Id, out var rootPath, out _))
					if (Directory.Exists(rootPath))
						Directory.Delete(rootPath, true);

				foreach (var (id, data) in backupData.Files)
				{
					if (!_pathBuilder.TryGetPrefixedPath(_lastTree, id, out var path, out _)) continue;

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
	}
}