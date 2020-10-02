﻿namespace Commands
{
	public interface ICommandVisitor
	{
		void Visit(ICommand command);
		void Visit(ExitCommand command);
		void Visit(CreateFileCommand command);
		void Visit(DeleteCommand command);
		void Visit(MakeDirectoryCommand command);
	}
}