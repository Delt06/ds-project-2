using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Commands;
using Commands.Serialization;
using Files;
using Networking;
using File = System.IO.File;

namespace FileServer
{
	public sealed class BackupWorker
	{
		private readonly object _mutex;
		private readonly Func<INode?> _getLastTree;
		private readonly FilePathBuilder _pathBuilder;
		private readonly Func<Timestamp?> _getTimestamp;

		public BackupWorker(object mutex, Func<INode?> getLastTree, FilePathBuilder pathBuilder, Func<Timestamp?> getTimestamp)
		{
			_mutex = mutex;
			_getLastTree = getLastTree;
			_pathBuilder = pathBuilder;
			_getTimestamp = getTimestamp;
		}

		public void LaunchOn(int port)
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

						lock (_mutex)
						{
							var tree = LastTree?.Clone();
							var timeStamp = Timestamp?.Clone();
							var backupData = new BackupData(tree, timeStamp);

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

		private INode? LastTree => _getLastTree();
		private Timestamp? Timestamp => _getTimestamp();
		
		private IEnumerable<(int id, byte[] data)> GetFiles(INode root)
		{
			foreach (var node in Traverse(root))
			{
				if (!TryGetPrefixedPath(root, node.Id, out var path)) continue;

				if (File.Exists(path))
					yield return (node.Id, File.ReadAllBytes(path));
			}
		}

		private bool TryGetPrefixedPath(INode root, int nodeId, out string path)
		{
			return _pathBuilder.TryGetPrefixedPath(root, nodeId, out path, out _);
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
	}
}