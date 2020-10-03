namespace Files
{
	public class TreeFactory
	{
		private int _lastId;

		public File CreateFile(string name, int size) => new File(_lastId++, name, size);
		public Directory CreateDirectory(string name) => new Directory(_lastId++, name);
		public void Reset() => _lastId = 0;
	}
}