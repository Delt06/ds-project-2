using System;
using System.Net;
using Commands;
using Networking;

namespace Client
{
	class Program
	{
		static void Main(string[] args)
		{
			var address = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(address, 55555);
			var remote = new IPEndPoint(address, 55556);
			using var connection = new Connection(local, remote);

			while (true)
			{
				Console.Write("Enter command: ");

				var input = Console.ReadLine();
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

					default:
						Console.WriteLine("Invalid input.");
						continue;
				}

				connection.Send(command);
				Console.WriteLine(connection.Receive());

				if (command is ExitCommand)
					return;
			}
		}
	}
}