using System;
using Files;

namespace Commands
{
	[Serializable]
	public sealed class FileMoveCommand : ICommand
	{
		public int FileId { get; set; }
		public int DestinationDirectoryId { get; set; }
		public INode? PreMoveTree { get; set; }

		public FileMoveCommand(int fileId, int destinationDirectoryId)
		{
			FileId = fileId;
			DestinationDirectoryId = destinationDirectoryId;
		}

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() =>
			$"FileMove FileID={FileId} DestinationDirectoryID={DestinationDirectoryId}";
	}
}