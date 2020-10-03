using System;

namespace Commands
{
	[Serializable]
	public sealed class FileCopyCommand : ICommand
	{
		public int FileId { get; set; }
		public string CopyName { get; set; }

		public FileCopyCommand(int fileId, string copyName)
		{
			FileId = fileId;
			CopyName = copyName;
		}

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"FileCopy FileID={FileId} CopyName={CopyName}";
	}
}