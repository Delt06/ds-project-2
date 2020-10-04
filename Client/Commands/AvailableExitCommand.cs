using Commands;

namespace Client.Commands
{
	public sealed class AvailableExitCommand : AvailableCommand
	{
		public override string Name => "Exit";

		public override bool TryHandle(out ICommand result)
		{
			result = new ExitCommand();
			return true;
		}
	}
}