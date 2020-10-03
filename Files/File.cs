using System;
using System.Collections.Generic;

namespace Files
{
	[Serializable]
	public sealed class File : INode
	{
		public int Id { get; }
		public string Name { get; }

		public File(int id, string name)
		{
			Id = id;
			Name = name;
		}

		public IEnumerable<INode> Children => Array.Empty<INode>();

		public INode Clone() => new File(Id, Name);

		object ICloneable.Clone() => Clone();
	}
}