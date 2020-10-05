using System;
using System.Linq;
using Commands;
using Files;

namespace NameServer
{
	public class ExecuteCommandVisitor : ICommandVisitor
	{
		private readonly Func<INode> _getRoot;
		private readonly TreeFactory _factory;
		private readonly Action _initialize;
		private readonly Timestamp _timestamp;

		public ExecuteCommandVisitor(Func<INode> getRoot, TreeFactory factory, Action initialize, Timestamp timestamp)
		{
			_getRoot = getRoot;
			_factory = factory;
			_initialize = initialize;
			_timestamp = timestamp;
		}
			
		public bool Exit { get; private set; }
		public string? Message { get; private set; }
		public bool AwaitResponse { get; private set; }

		private INode Root => _getRoot();

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

			if (Root.TryFindNode(command.DirectoryId, out var node) &&
			    node is Directory directory)
			{
				var existingFile = directory.Children.FirstOrDefault(c => c.Name == command.Name);
				if (existingFile == null)
				{
					var file = _factory.CreateFile(command.Name, 0);
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

			if (Root.TryFindNode(command.NodeId, out var node) &&
			    Root.TryFindParent(command.NodeId, out var parent) &&
			    parent is Directory parentDirectory)
			{
				parentDirectory.Children.Remove(node);
				_timestamp.Increment();
			}
			else
			{
				Message = $"Node with {command.NodeId} either does not exist or has no parent.";
			}
		}

		public void Visit(MakeDirectoryCommand command)
		{
			Visit((ICommand) command);

			if (Root.TryFindNode(command.ParentDirectoryId, out var parent) &&
			    parent is Directory parentDirectory)
			{
				var existingDirectory = parentDirectory.Children.FirstOrDefault(c => c.Name == command.Name);
				if (existingDirectory == null)
				{
					var directory = _factory.CreateDirectory(command.Name);
					parentDirectory.Children.Add(directory);
					_timestamp.Increment();
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

			if (Root.TryFindNode(command.DirectoryId, out var parent) &&
			    parent is Directory parentDirectory)
			{
				var existingFile = parentDirectory.Children.FirstOrDefault(c => c.Name == command.Name);
				if (existingFile == null)
				{
					var file = _factory.CreateFile(command.Name, command.Data.Length);
					parentDirectory.Children.Add(file);
					_timestamp.Increment();
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
			_initialize();
		}

		public void Visit(ReadDirectoryCommand command)
		{
			Visit((ICommand) command);

			if (Root.TryFindNode(command.Id, out var node) &&
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

			if (Root.TryFindNode(command.Id, out var node) &&
			    node is File)
			{
				AwaitResponse = true;
			}
			else
			{
				OnFileDoesNotExist(command.Id);
			}
		}

		public void Visit(FileInfoCommand command)
		{
			Visit((ICommand) command);

			if (Root.TryFindNode(command.Id, out var node) &&
			    node is File file)
			{
				Message = $"ID={file.Id} Name={file.Name} Size={file.Size}";
			}
			else
			{
				OnFileDoesNotExist(command.Id);
			}
		}

		public void Visit(FileCopyCommand command)
		{
			Visit((ICommand) command);

			if (Root.TryFindNode(command.FileId, out var node) &&
			    node is File file)
			{
				if (Root.TryFindParent(command.FileId, out var parent) &&
				    parent is Directory parentDirectory)
				{
					var existingFile = parentDirectory.Children.FirstOrDefault(c => c.Name == command.CopyName);

					if (existingFile != null)
					{
						OnNodeAlreadyExists(command.CopyName, parentDirectory.Id);
					}
					else
					{
						var copy = _factory.CreateFile(command.CopyName, file.Size);
						parentDirectory.Children.Add(copy);
						_timestamp.Increment();
						Message = $"ID={copy.Id}";
					}
				}
				else
				{
					Message = $"File with ID {command.FileId} has no parent.";
				}
				
			}
			else
			{
				OnFileDoesNotExist(command.FileId);
			}
		}

		public void Visit(FileMoveCommand command)
		{
			Visit((ICommand) command);
			command.PreMoveTree = Root.Clone();

			if (Root.TryFindNode(command.FileId, out var file) &&
			    file is File)
			{
				if (Root.TryFindParent(command.FileId, out var directoryNode) &&
				    directoryNode is Directory directory)
				{
					if (Root.TryFindNode(command.DestinationDirectoryId, out var destinationDirectoryNode) &&
					    destinationDirectoryNode is Directory destinationDirectory)
					{
						directory.Children.Remove(file);
						destinationDirectory.Children.Add(file);
						_timestamp.Increment();
					}
					else
					{
						OnDirectoryDoesNotExist(command.DestinationDirectoryId);
					}
				}
				else
				{
					Message = $"Node with {command.FileId} has no parent.";
				}
			}
			else
			{
				OnFileDoesNotExist(command.FileId);
			}
		}

		private void OnFileDoesNotExist(int fileId)
		{
			Message = $"File with ID {fileId} does not exist.";
		}
	}
}