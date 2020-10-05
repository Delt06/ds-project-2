using System;

namespace Commands
{
	[Serializable]
	public sealed class Timestamp : ICloneable, IEquatable<Timestamp>
	{ 
		public int Value { get; private set; }
		public void Reset() => Value = 0;
		public void Increment() => Value++;

		public Timestamp Clone()
		{
			return new Timestamp {Value = Value};
		}

		object ICloneable.Clone() => Clone();

		public bool Equals(Timestamp? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Value == other.Value;
		}

		public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Timestamp other && Equals(other);

		public override int GetHashCode() => Value;
	}
}