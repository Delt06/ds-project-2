using Commands;

namespace Client.Commands
{
	public sealed class AvailableInitializeCommand : AvailableCommand
	{
		public override string Name => "Initialize";

		public override bool TryHandle(out ICommand result)
		{
			result = new InitializeCommand();
			return true;
		}
	}
}