namespace Commands
{
	public interface ICommand
	{
		void Accept(ICommandVisitor visitor);
	}
}