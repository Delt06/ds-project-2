using System;
using System.Collections.Generic;

namespace Files
{
	[Serializable]
	public sealed class Directory : INode
	{
		public int Id { get; }
		public string Name { get; }

		public Directory(int id, string name)
		{
			Id = id;
			Name = name;
		}

		IEnumerable<INode> INode.Children => Children;

		public INode Clone()
		{
			var clone = new Directory(Id, Name);

			foreach (var child in Children)
			{
				var childClone = child.Clone();
				clone.Children.Add(childClone);
			}

			return clone;
		}

		public readonly List<INode> Children = new List<INode>();
		object ICloneable.Clone() => Clone();
	}
}