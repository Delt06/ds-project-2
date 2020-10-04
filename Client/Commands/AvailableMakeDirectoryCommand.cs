using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableMakeDirectoryCommand : AvailableCommand
	{
		public override string Name => "MakeDirectory";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter parent directory ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			Console.Write("Enter directory name: ");
			var directoryName = Console.ReadLine() ?? string.Empty;

			result = new MakeDirectoryCommand(id, directoryName);
			return true;
		}
	}
}