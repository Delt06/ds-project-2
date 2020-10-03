using System;

namespace Commands
{
	[Serializable]
	public sealed class CreateFileCommand : ICommand
	{
		public int DirectoryId { get; set; } = 0;
		public string Name { get; set; } = "new_file";

		public CreateFileCommand(int directoryId, string name)
		{
			DirectoryId = directoryId;
			Name = name;
		}

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"CreateFile DirectoryID={DirectoryId} Name={Name}";
	}
}