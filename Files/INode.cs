using System.Collections.Generic;

namespace Files
{
	public interface INode
	{
		int Id { get; }
		string Name { get; }
		IEnumerable<INode> Children { get; }
	}
}