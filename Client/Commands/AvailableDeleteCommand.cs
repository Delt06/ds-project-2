using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableDeleteCommand : AvailableCommand
	{
		public override string Name => "Delete";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter node ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			result = new DeleteCommand(id);
			return true;
		}
	}
}