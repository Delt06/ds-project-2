using System.IO;
using Files;

namespace FileServer
{
	public sealed class FilePathBuilder
	{
		private readonly string _pathPrefix;

		public FilePathBuilder(string pathPrefix) => _pathPrefix = pathPrefix;

		public bool TryGetPrefixedPath(INode root, int nodeId, out string path, out string normalPath)
		{
			if (!TryGetPathTo(root, nodeId, out normalPath))
			{
				path = default!;
				return false;
			}

			path = _pathPrefix + normalPath;
			return true;
		}

		public static bool TryGetPathTo(INode root, int nodeId, out string path)
		{
			if (root.Id == nodeId)
			{
				path = root.Name;
				return true;
			}

			foreach (var child in root.Children)
			{
				if (!TryGetPathTo(child, nodeId, out var pathEnd)) continue;

				path = Path.Combine(root.Name, pathEnd);
				return true;
			}

			path = default!;
			return false;
		}
	}
}