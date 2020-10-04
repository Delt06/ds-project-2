using System.IO;
using Commands;
using Files;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace FileServer
{
	public class CommandHandleVisitor : ICommandVisitor
	{
		public byte[]? Payload { get; private set; }
		public string PayloadPath { get; private set; } = string.Empty;

		private readonly INode _root;
		private readonly FilePathBuilder _pathBuilder;

		public CommandHandleVisitor(INode root, FilePathBuilder pathBuilder)
		{
			_root = root;
			_pathBuilder = pathBuilder;
		}

		public void Visit(ICommand command) { }

		public void Visit(ExitCommand command) { }

		public void Visit(CreateFileCommand command)
		{
			if (!TryGetPrefixedPathTo(command.DirectoryId, out var pathToDirectory)) return;

			var path = Path.Combine(pathToDirectory, command.Name);
			Directory.CreateDirectory(pathToDirectory);

			if (File.Exists(path))
				File.Delete(path);

			using var file = File.Create(path);
		}

		public void Visit(DeleteCommand command)
		{
			if (!TryGetPrefixedPathTo(command.NodeId, out var path)) return;

			if (File.Exists(path))
				File.Delete(path);
			else if (Directory.Exists(path))
				Directory.Delete(path, true);
		}

		public void Visit(MakeDirectoryCommand command)
		{
			if (!TryGetPrefixedPathTo(command.ParentDirectoryId, out var pathToParent)) return;

			var path = Path.Combine(pathToParent, command.Name);
			Directory.CreateDirectory(path);
		}

		public void Visit(UploadFileCommand command)
		{
			if (!TryGetPrefixedPathTo(command.DirectoryId, out var directoryPath)) return;

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
			if (!TryGetPrefixedPathTo(_root.Id, out var path)) return;

			if (Directory.Exists(path))
				Directory.Delete(path, true);
		}

		public void Visit(ReadDirectoryCommand command) { }

		public void Visit(DownloadFileCommand command)
		{
			if (!TryGetPrefixedPathTo(command.Id, out var path, out var normalPath)) return;
			if (!File.Exists(path)) return;

			Payload = File.ReadAllBytes(path);
			PayloadPath = normalPath;
		}

		public void Visit(FileInfoCommand command) { }

		public void Visit(FileCopyCommand command)
		{
			if (!TryGetPrefixedPathTo(command.FileId, out var path)) return;

			var directory = Path.GetDirectoryName(path) ?? string.Empty;
			var fileData = File.ReadAllBytes(path);
			var copyPath = Path.Combine(directory, command.CopyName);

			if (!File.Exists(copyPath))
				File.WriteAllBytes(copyPath, fileData);
		}

		public void Visit(FileMoveCommand command)
		{
			if (command.PreMoveTree == null) return;
			if (!TryGetPrefixedPathTo(command.PreMoveTree, command.FileId, out var filePath)) return;
			if (!TryGetPrefixedPathTo(command.PreMoveTree, command.DestinationDirectoryId,
				out var destinationDirectoryPath)) return;

			var fileName = Path.GetFileName(filePath);
			var destinationFilePath = Path.Combine(destinationDirectoryPath, fileName);
			File.Move(filePath, destinationFilePath);
		}

		private bool TryGetPrefixedPathTo(int nodeId, out string path) =>
			TryGetPrefixedPathTo(nodeId, out path, out _);

		private bool TryGetPrefixedPathTo(int nodeId, out string path, out string normalPath) =>
			_pathBuilder.TryGetPrefixedPath(_root, nodeId, out path, out normalPath);

		private bool TryGetPrefixedPathTo(INode root, int nodeId, out string path) =>
			_pathBuilder.TryGetPrefixedPath(root, nodeId, out path, out _);
	}
}