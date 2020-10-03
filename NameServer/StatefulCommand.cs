using System;
using Commands;
using Files;

namespace NameServer
{
	[Serializable]
	public sealed class StatefulCommand : ICommand
	{ 
		public INode Root { get; set; }
		public ICommand InnerCommand { get; set; }

		public StatefulCommand(INode root, ICommand innerCommand)
		{
			Root = root;
			InnerCommand = innerCommand;
		}

		public void Accept(ICommandVisitor visitor)
		{
			InnerCommand.Accept(visitor);
		}

		public override string ToString() => $"Stateful {{{InnerCommand}}}";
	}
}