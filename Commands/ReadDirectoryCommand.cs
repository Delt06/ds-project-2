using System;

namespace Commands
{
	[Serializable]
	public sealed class ReadDirectoryCommand : ICommand
	{
		public int DirectoryId { get; set; }

		public ReadDirectoryCommand(int directoryId) => DirectoryId = directoryId;
		
		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}
	}
}