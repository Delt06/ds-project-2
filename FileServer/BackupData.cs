using System;
using System.Collections.Generic;
using Files;

namespace FileServer
{
	[Serializable]
	public class BackupData
	{
		public INode? Tree { get; set; }
		public List<(int id, byte[] data)> Files { get; set; } = new List<(int id, byte[] data)>();

		public BackupData(INode? tree) => Tree = tree;
	}
}