using System;
using Commands;

namespace Client.Commands
{
	public class AvailableDownloadFileCommand : AvailableCommand
	{
		public override string Name => "DownloadFile";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter file ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
				return false;

			result = new DownloadFileCommand(id);
			return true;
		}
	}
}