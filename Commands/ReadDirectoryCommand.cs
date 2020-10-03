using System;

namespace Commands
{
	[Serializable]
	public sealed class ReadDirectoryCommand : ICommand
	{
		public int Id { get; set; }

		public ReadDirectoryCommand(int id) => Id = id;

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"ReadDirectory ID={Id}";
	}
}