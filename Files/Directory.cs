using System;
using System.Collections.Generic;
using System.Linq;

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

		public bool Equals(INode? other) => Equals((object?) other);

		private bool Equals(Directory other) => Children.ToHashSet().SetEquals(other.Children.ToHashSet()) &&
		                                        Id == other.Id && Name == other.Name;

		public override bool Equals(object? obj) =>
			ReferenceEquals(this, obj) || obj is Directory other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Children, Id, Name);

		public override string ToString() => $"{Id}: {{{string.Join(", ", Children)}}}";
	}
}