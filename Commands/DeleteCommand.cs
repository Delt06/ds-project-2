using System;

namespace Commands
{
	[Serializable]
	public sealed class DeleteCommand : ICommand
	{
		public int NodeId { get; set; }

		public DeleteCommand(int nodeId) => NodeId = nodeId;

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"Delete ID={NodeId}";
	}
}