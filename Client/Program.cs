using System;
using System.IO;
using System.Net;
using Commands;
using Networking;

namespace Client
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			var address = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(address, 55555);
			var remote = new IPEndPoint(address, 55556);
			using var connection = new Connection(local, remote);

			while (true)
			{
				Console.Write("Enter command: ");

				var input = Console.ReadLine()?.Trim() ?? string.Empty;
				ICommand command;

				switch (input)
				{
					case "Exit":
						command = new ExitCommand();
						break;

					case "CreateFile":
					{
						Console.Write("Enter directory ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						Console.Write("Enter file name: ");
						var fileName = Console.ReadLine() ?? string.Empty;
						command = new CreateFileCommand(id, fileName);
						break;
					}


					case "Delete":
					{
						Console.Write("Enter node ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						command = new DeleteCommand(id);
						break;
					}

					case "MakeDirectory":
					{
						Console.Write("Enter parent directory ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						Console.Write("Enter directory name: ");
						var directoryName = Console.ReadLine() ?? string.Empty;

						command = new MakeDirectoryCommand(id, directoryName);
						break;
					}

					case "UploadFile":
					{
						Console.Write("Enter directory ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						Console.Write("Enter file path: ");
						var path = Console.ReadLine() ?? string.Empty;
						if (!File.Exists(path))
						{
							Console.WriteLine($"File at {path} does not exist.");
							goto default;
						}

						var data = File.ReadAllBytes(path);
						var fileName = Path.GetFileName(path);
						command = new UploadFileCommand(id, fileName, data);
						break;
					}

					case "Initialize":
						command = new InitializeCommand();
						break;

					case "ReadDirectory":
					{
						Console.Write("Enter directory ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						command = new ReadDirectoryCommand(id);
						break;
					}

					case "DownloadFile":
					{
						Console.Write("Enter file ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						command = new DownloadFileCommand(id);
						break;
					}

					case "FileInfo":
					{
						Console.Write("Enter file ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						command = new FileInfoCommand(id);
						break;
					}

					case "FileCopy":
					{
						Console.Write("Enter file ID: ");
						if (!int.TryParse(Console.ReadLine(), out var id))
							goto default;

						Console.Write("Enter copy name: ");
						var copyName = Console.ReadLine() ?? string.Empty;

						command = new FileCopyCommand(id, copyName);
						break;
					}

					default:
						Console.WriteLine("Invalid input.");
						continue;
				}

				connection.Send(command);
				var response = connection.Receive();
				Console.WriteLine(response);

				if (response is PayloadResponseCommand payloadResponse)
				{
					Console.WriteLine(payloadResponse.PayloadPath);

					var directory = Path.GetDirectoryName(payloadResponse.PayloadPath);
					if (!string.IsNullOrWhiteSpace(directory))
						Directory.CreateDirectory(directory);

					File.WriteAllBytes(payloadResponse.PayloadPath, payloadResponse.Payload);
				}

				if (command is ExitCommand)
					return;
			}
		}
	}
}