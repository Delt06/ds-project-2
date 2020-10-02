using System;
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
		private static void Main(string[] args)
		{
			using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			var address = IpAddressUtils.GetLocal();
			var endpoint = new IPEndPoint(address, 55556);
			socket.Bind(endpoint);
			socket.Listen(1);
			
			var factory = new TreeFactory();
			var root = factory.CreateDirectory("root");

			while (true)
			{
				using var client = socket.Accept();
				
				Console.WriteLine("A client has connected.");
				HandleClient(client, root, factory);
				Console.WriteLine("A client has disconnected.");
			}
		}

		private static void HandleClient(Socket client, INode root, TreeFactory treeFactory)
		{
			var visitor = new ExecuteCommandVisitor(root, treeFactory);
			
			while (!visitor.Exit)
			{
				var data = client.ReceiveUntilEof();
				var receivedCommand = data.To<ICommand>();
				
				Console.WriteLine(receivedCommand);
				receivedCommand.Accept(visitor);

				var response = new ResponseCommand(receivedCommand, visitor.Message);
				client.SendCompletely(response.ToBytes());
				client.SendCompletely(Conventions.Eof);
			}
		}

		private class ExecuteCommandVisitor : ICommandVisitor
		{
			private readonly INode _root;
			private readonly TreeFactory _factory;

			public ExecuteCommandVisitor(INode root, TreeFactory factory)
			{
				_root = root;
				_factory = factory;
			}

			public bool Exit { get; private set; }
			public string? Message { get; private set; }
			
			public void Visit(ICommand command)
			{
				Exit = false;
				Message = null;
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
					var file = _factory.CreateFile(command.Name);
					directory.Children.Add(file);
					Message = $"ID={file.Id}";
				}
				else
				{
					Message = $"Directory with ID {command.DirectoryId} does not exist.";
				}
			}

			public void Visit(DeleteCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.NodeId, out var node) &&
					_root.TryFindParent(command.NodeId, out var parent) &&
				    parent is Directory parentDirectory)
				{
					parentDirectory.Children.Remove(node);
				}
				else
				{
					Message = $"Node with {command.NodeId} either does not exist or has no parent.";
				}
			}

			public void Visit(MakeDirectoryCommand command)
			{
				Visit((ICommand) command);

				if (_root.TryFindNode(command.ParentDirectoryId, out var parent) &&
				    parent is Directory parentDirectory)
				{
					var directory = _factory.CreateDirectory(command.Name);
					parentDirectory.Children.Add(directory);
					Message = $"ID={directory.Id}";
				}
				else
				{
					Message = $"Directory with ID {command.ParentDirectoryId} does not exist.";
				}
			}
		}
	}
}