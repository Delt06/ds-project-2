using System;
using Files;

namespace Commands
{
	[Serializable]
	public sealed class StatefulCommand : ICommand
	{
		public INode Root { get; set; }
		public ICommand InnerCommand { get; set; }
		public Timestamp Timestamp { get; set; }

		public StatefulCommand(INode root, ICommand innerCommand, Timestamp timestamp)
		{
			Root = root;
			InnerCommand = innerCommand;
			Timestamp = timestamp;
		}

		public void Accept(ICommandVisitor visitor)
		{
			InnerCommand.Accept(visitor);
		}

		public override string ToString() => $"Stateful {{{InnerCommand}}}";
	}
}