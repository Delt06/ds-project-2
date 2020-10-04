using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableCreateFileCommand : AvailableCommand
	{
		public override string Name => "CreateFile";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter directory ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			Console.Write("Enter file name: ");
			var fileName = Console.ReadLine() ?? string.Empty;
			result = new CreateFileCommand(id, fileName);
			return true;
		}
	}
}