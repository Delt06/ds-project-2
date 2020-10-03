namespace Files
{
	public class TreeFactory
	{
		private int _lastId;

		public File CreateFile(string name) => new File(_lastId++, name);
		public Directory CreateDirectory(string name) => new Directory(_lastId++, name);
		public void Reset() => _lastId = 0;
	}
}