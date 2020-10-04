using System;
using System.Text;

namespace Networking
{
	public static class Conventions
	{
		public const int NameServerPort = 55556;
		
		private const string EofText = "<EOF>";
		private static readonly byte[] EofBytes = Encoding.ASCII.GetBytes(EofText);

		public static ReadOnlySpan<byte> Eof => EofBytes;

		public static bool ContainsEof(byte[] buffer, int length)
		{
			for (var start = 0; start < length - EofBytes.Length + 1; start++)
			{
				var correct = true;

				for (var index = 0; index < EofBytes.Length; index++)
				{
					var totalIndex = start + index;
					if (buffer[totalIndex] == EofBytes[index]) continue;

					correct = false;
					break;
				}

				if (correct)
					return true;
			}

			return false;
		}
	}
}