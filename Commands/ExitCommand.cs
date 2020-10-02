using System;

namespace Commands
{
	[Serializable]
	public class ExitCommand : ICommand
	{
		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => "Exit";
	}
}