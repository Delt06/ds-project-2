using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableReadDirectoryCommand : AvailableCommand
	{
		public override string Name => "ReadDirectory";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter directory ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			result = new ReadDirectoryCommand(id);
			return true;
		}
	}
}