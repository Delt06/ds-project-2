using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Commands.Serialization
{
	public static class SerializationExt
	{
		public static byte[] ToBytes(this object obj)
		{
			var serializer = new BinaryFormatter();
			var stream = new MemoryStream();
			serializer.Serialize(stream, obj);
			return stream.ToArray();
		}

		public static T To<T>(this ReadOnlySpan<byte> buffer)
		{
			var serializer = new BinaryFormatter();
			var stream = new MemoryStream(buffer.ToArray());
			return (T) serializer.Deserialize(stream);
		}
	}
}