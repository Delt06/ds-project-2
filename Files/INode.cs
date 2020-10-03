using System;
using System.Collections.Generic;

namespace Files
{
	public interface INode : ICloneable
	{
		int Id { get; }
		string Name { get; }
		IEnumerable<INode> Children { get; }

		new INode Clone();
	}
}