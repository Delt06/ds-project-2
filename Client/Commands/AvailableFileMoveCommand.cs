using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableFileMoveCommand : AvailableCommand
	{
		public override string Name => "FileMove";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter file ID: ");
			if (!int.TryParse(Console.ReadLine(), out var fileId))
				return false;

			Console.Write("Enter destination directory ID: ");
			if (!int.TryParse(Console.ReadLine(), out var destinationDirectoryId))
				return false;

			result = new FileMoveCommand(fileId, destinationDirectoryId);
			return true;
		}
	}
}