using System;
using System.IO;
using System.Net;
using Client.Commands;
using Commands;
using Networking;

namespace Client
{
	internal class Program
	{
		public const int Port = 55555;

		private static readonly AvailableCommand[] AvailableCommands =
		{
			new AvailableInitializeCommand(),
			
			new AvailableCreateFileCommand(),
			new AvailableUploadFileCommand(),
			new AvailableDownloadFileCommand(),
			new AvailableDeleteCommand(),
			
			new AvailableFileInfoCommand(),
			new AvailableFileCopyCommand(),
			new AvailableFileMoveCommand(),
			
			new AvailableMakeDirectoryCommand(),
			new AvailableReadDirectoryCommand(), 
			new AvailableExitCommand(),
		};
		
		private static void Main(string[] args)
		{
			var address = IpAddressUtils.GetLocal();
			var local = new IPEndPoint(address, Port);
			Console.WriteLine($"Local address={local}");
			
			Console.Write("Input the IP address of the name server: ");
			var remoteAddress = IPAddress.Parse(Console.ReadLine() ?? string.Empty);
			var remote = new IPEndPoint(remoteAddress, Conventions.NameServerPort);
			using var connection = new Connection(local, remote);
			
			Console.WriteLine("Available commands: ");

			foreach (var availableCommand in AvailableCommands)
			{
				Console.WriteLine(availableCommand.Name);
			}

			while (true)
			{
				Console.Write("Enter command: ");
				var commandName = Console.ReadLine()?.Trim() ?? string.Empty;
				
				if (TryHandle(commandName, out var command))
				{
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
				else
				{
					Console.WriteLine("Invalid input.");
				}
			}
		}

		private static bool TryHandle(string name, out ICommand command)
		{
			command = default!;
			return TryResolveAvailableCommand(name, out var availableCommand) &&
			       availableCommand.TryHandle(out command);
		}

		private static bool TryResolveAvailableCommand(string name, out AvailableCommand resolvedAvailableCommand)
		{
			foreach (var availableCommand in AvailableCommands)
			{
				if (!name.Equals(availableCommand.Name)) continue;
				
				resolvedAvailableCommand = availableCommand;
				return true;
			}

			resolvedAvailableCommand = default!;
			return false;
		}
	}
}