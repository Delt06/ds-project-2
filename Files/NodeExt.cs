namespace Files
{
	public static class NodeExt
	{
		public static bool TryFindNode(this INode root, int nodeId, out INode node)
		{
			if (root.Id == nodeId)
			{
				node = root;
				return true;
			}

			foreach (var child in root.Children)
			{
				if (TryFindNode(child, nodeId, out node))
					return true;
			}

			node = default!;
			return false;
		}

		public static bool TryFindParent(this INode root, int childId, out INode parent)
		{
			if (root.Id == childId)
			{
				parent = default!;
				return false;
			}

			foreach (var child in root.Children)
			{
				if (child.Id == childId)
				{
					parent = root;
					return true;
				}

				if (TryFindNode(child, childId, out parent)) return true;
			}

			parent = default!;
			return false;
		}
	}
}