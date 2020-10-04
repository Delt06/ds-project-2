using Commands;

namespace Client.Commands
{
	public abstract class AvailableCommand
	{
		public abstract string Name { get; }

		public abstract bool TryHandle(out ICommand result);
	}
}