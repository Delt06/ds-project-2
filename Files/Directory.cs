using System.Collections.Generic;

namespace Files
{
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
		
		public readonly ISet<INode> Children = new HashSet<INode>();
	}
}