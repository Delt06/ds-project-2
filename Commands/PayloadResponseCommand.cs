using System;
using Files;

namespace Commands
{
	[Serializable]
	public sealed class PayloadResponseCommand : ResponseCommand
	{
		public byte[] Payload { get; set; }
		public string PayloadPath { get; set; }
		public INode Root { get; set; }

		public PayloadResponseCommand(ICommand request, byte[] payload, string payloadPath, INode root,
			string? message = null) : base(request, message)
		{
			Payload = payload;
			Root = root;
			PayloadPath = payloadPath;
		}

		public override string ToString() => $"{base.ToString()} PayloadSize={Payload.Length}";
	}
}