using System;

namespace Commands
{
	[Serializable]
	public class UploadFileCommand : ICommand
	{
		public int DirectoryId { get; set; } = 0;
		public string Name { get; set; } = "new_file";
		public byte[] Data { get; set; } = Array.Empty<byte>();

		public UploadFileCommand(int directoryId, string name, byte[] data)
		{
			DirectoryId = directoryId;
			Name = name;
			Data = data;
		}

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"UploadFile DirectoryID={DirectoryId} Name={Name} Size={Data.Length}";
	}
}