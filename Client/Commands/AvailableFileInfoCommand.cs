using System;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableFileInfoCommand : AvailableCommand
	{
		public override string Name => "FileInfo";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter file ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			result = new FileInfoCommand(id);
			return true;
		}
	}
}