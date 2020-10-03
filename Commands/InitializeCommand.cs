using System;

namespace Commands
{
	[Serializable]
	public sealed class InitializeCommand : ICommand
	{
		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => "Initialize";
	}
}