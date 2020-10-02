using System;

namespace Commands
{
	[Serializable]
	public class MakeDirectoryCommand : ICommand
	{
		public int ParentDirectoryId { get; set; }
		public string Name { get; set; } = "Dir";

		public MakeDirectoryCommand(int parentDirectoryId, string name)
		{
			ParentDirectoryId = parentDirectoryId;
			Name = name;
		}

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}
	}
}