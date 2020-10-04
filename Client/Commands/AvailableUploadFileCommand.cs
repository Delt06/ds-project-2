using System;
using System.IO;
using Commands;

namespace Client.Commands
{
	public sealed class AvailableUploadFileCommand : AvailableCommand
	{
		public override string Name => "UploadFile";

		public override bool TryHandle(out ICommand result)
		{
			result = default!;
			
			Console.Write("Enter directory ID: ");
			if (!int.TryParse(Console.ReadLine(), out var id))
			{
				return false;
			}

			Console.Write("Enter file path: ");
			var path = Console.ReadLine() ?? string.Empty;
			if (!File.Exists(path))
			{
				Console.WriteLine($"File at {path} does not exist.");
				return false;
			}

			var data = File.ReadAllBytes(path);
			var fileName = Path.GetFileName(path);
			result = new UploadFileCommand(id, fileName, data);
			return true;
		}
	}
}