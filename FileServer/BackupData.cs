using System;
using System.Collections.Generic;
using Commands;
using Files;

namespace FileServer
{
	[Serializable]
	public class BackupData
	{
		public INode? Tree { get; set; }
		public Timestamp? Timestamp { get; set; }
		public List<(int id, byte[] data)> Files { get; set; } = new List<(int id, byte[] data)>();

		public BackupData(INode? tree, Timestamp? timestamp)
		{
			Tree = tree;
			Timestamp = timestamp;
		}
	}
}