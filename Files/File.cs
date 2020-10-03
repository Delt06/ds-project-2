using System;
using System.Collections.Generic;

namespace Files
{
	[Serializable]
	public sealed class File : INode
	{
		public int Id { get; }
		public string Name { get; }
		public int Size { get; }

		public File(int id, string name, int size)
		{
			Id = id;
			Name = name;
			Size = size;
		}

		IEnumerable<INode> INode.Children => Array.Empty<INode>();

		public INode Clone() => new File(Id, Name, Size);

		object ICloneable.Clone() => Clone();

		public bool Equals(INode? other) => Equals((object?) other);

		private bool Equals(File other) => Id == other.Id &&
		                                   Name == other.Name &&
		                                   Size == other.Size;

		public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is File other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Id, Name);

		public override string ToString() => Id.ToString();
	}
}