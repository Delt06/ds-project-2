using System;
using System.Text;

namespace Networking
{
	public static class Conventions
	{
		private const string EofText = "<EOF>";
		private static readonly byte[] EofBytes = Encoding.ASCII.GetBytes(EofText);

		public static ReadOnlySpan<byte> Eof => EofBytes;

		public static bool ContainsEof(byte[] buffer, int length)
		{
			var data = Encoding.ASCII.GetString(buffer, 0, length);
			return data.IndexOf(EofText, StringComparison.Ordinal) > -1;
		}
	}
}