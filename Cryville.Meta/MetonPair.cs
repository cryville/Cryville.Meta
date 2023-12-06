using System;

namespace Cryville.Meta {
	public record struct MetonPair(Meton Key, Meton Value) : IEquatable<MetonPair> {
		/// <inheritdoc />
		public readonly bool Equals(MetonPair other) => Key == other.Key && Value == other.Value;
		/// <inheritdoc />
		public override readonly int GetHashCode() {
			var hv = Value.GetHashCode();
			return Key.GetHashCode() ^ (hv << 16 | hv >> 16);
		}
	}
}
