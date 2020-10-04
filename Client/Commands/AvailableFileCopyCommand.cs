using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableFileCopyCommand : AvailableCommand
	{
		public override string Name => "FileCopy";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter file ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			Console.Write("Enter copy name: ");
			var copyName = Console.ReadLine() ?? string.Empty;

			result = new FileCopyCommand(id, copyName);
			return true;
		}
	}
}