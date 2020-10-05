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
		public Timestamp Timestamp { get; set; }

		public PayloadResponseCommand(ICommand request, byte[] payload, string payloadPath, INode root, Timestamp timestamp, string? message = null) : base(request, message)
		{
			Payload = payload;
			Root = root;
			Timestamp = timestamp;
			PayloadPath = payloadPath;
		}

		public override string ToString() => $"{base.ToString()} PayloadSize={Payload.Length}";
	}
}