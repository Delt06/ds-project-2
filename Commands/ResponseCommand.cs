using System;

namespace Commands
{
	[Serializable]
	public sealed class ResponseCommand : ICommand
	{
		public string? Message { get; set; } 
		public ICommand Request { get; set; }

		public ResponseCommand(ICommand request, string? message = null)
		{
			Request = request;
			Message = message;
		}

		public void Accept(ICommandVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override string ToString() => $"Response Message={{{Message}}} {{{Request}}}";
	}
}