using System;

namespace Commands
{
	[Serializable]
	public sealed class DownloadFileCommand : ICommand
	{
		public int Id { get; set; }

		public DownloadFileCommand(int nodeId) => Id = nodeId;
		
		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"DownloadFile ID={Id}";
	}
}