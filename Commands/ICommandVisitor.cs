namespace Commands
{
	public interface ICommandVisitor
	{
		void Visit(ICommand command);
		void Visit(ExitCommand command);
		void Visit(CreateFileCommand command);
		void Visit(DeleteCommand command);
		void Visit(MakeDirectoryCommand command);
		void Visit(UploadFileCommand command);
		void Visit(InitializeCommand command);
		void Visit(ReadDirectoryCommand command);
		void Visit(DownloadFileCommand command);
		void Visit(FileInfoCommand command);
	}
}