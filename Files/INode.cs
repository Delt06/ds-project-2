using System;
using System.Collections.Generic;

namespace Files
{
	public interface INode : ICloneable, IEquatable<INode>
	{
		int Id { get; }
		string Name { get; }
		IEnumerable<INode> Children { get; }

		new INode Clone();
	}
}