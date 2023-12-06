using System;
using System.IO;

namespace Cryville.Meta.Model {
	public struct MetonIdentifier : IModel, IComparable<MetonIdentifier>, IEquatable<MetonIdentifier> {
		public ulong TypeKey { get; set; }
		public ulong SubKey1 { get; set; }
		public ulong SubKey2 { get; set; }
		public ulong SubKey3 { get; set; }
		public ulong SubKey4 { get; set; }

		const ulong _pairTypeMask = 0xc000_0000_0000_0000U;
		internal readonly MetonPairType PairType => (MetonPairType)(TypeKey >> 62);
		public ulong TypeId {
			readonly get => TypeKey & ~_pairTypeMask;
			set {
				if ((value & _pairTypeMask) != 0) throw new ArgumentOutOfRangeException(nameof(value));
				TypeKey = (TypeKey & _pairTypeMask) | value;
			}
		}

		public readonly int CompareTo(MetonIdentifier other) {
			int r = TypeKey.CompareTo(other.TypeKey);
			if (r != 0) return r;
			r = SubKey1.CompareTo(other.SubKey1);
			if (r != 0) return r;
			r = SubKey2.CompareTo(other.SubKey2);
			if (r != 0) return r;
			r = SubKey3.CompareTo(other.SubKey3);
			if (r != 0) return r;
			return SubKey4.CompareTo(other.SubKey4);
		}

		void IModel.ReadFrom(BinaryReader reader) {
			TypeKey = reader.ReadUInt64();
			SubKey1 = reader.ReadUInt64();
			SubKey2 = reader.ReadUInt64();
			SubKey3 = reader.ReadUInt64();
			SubKey4 = reader.ReadUInt64();
		}
		readonly void IModel.WriteTo(BinaryWriter writer) {
			writer.Write(TypeKey);
			writer.Write(SubKey1);
			writer.Write(SubKey2);
			writer.Write(SubKey3);
			writer.Write(SubKey4);
		}

		/// <inheritdoc />
		public readonly bool Equals(MetonIdentifier other) =>
			TypeKey == other.TypeKey &&
			SubKey1 == other.SubKey1 &&
			SubKey2 == other.SubKey2 &&
			SubKey3 == other.SubKey3 &&
			SubKey4 == other.SubKey4;
		/// <inheritdoc />
		public override readonly bool Equals(object obj) => obj is MetonIdentifier other && Equals(other);

		/// <inheritdoc />
		public override readonly int GetHashCode() {
			var ret = TypeKey.GetHashCode();
			ret ^= SubKey1.GetHashCode();
			var h2 = SubKey2.GetHashCode();
			ret ^= h2 << 8 | h2 >> 24;
			var h3 = SubKey3.GetHashCode();
			ret ^= h3 << 16 | h3 >> 16;
			var h4 = SubKey4.GetHashCode();
			ret ^= h4 << 24 | h4 >> 8;
			return ret;
		}

		/// <inheritdoc />
		public static bool operator ==(MetonIdentifier left, MetonIdentifier right) => left.Equals(right);
		/// <inheritdoc />
		public static bool operator !=(MetonIdentifier left, MetonIdentifier right) => !(left == right);
		/// <inheritdoc />
		public static bool operator <(MetonIdentifier left, MetonIdentifier right) => left.CompareTo(right) < 0;
		/// <inheritdoc />
		public static bool operator <=(MetonIdentifier left, MetonIdentifier right) => left.CompareTo(right) <= 0;
		/// <inheritdoc />
		public static bool operator >(MetonIdentifier left, MetonIdentifier right) => left.CompareTo(right) > 0;
		/// <inheritdoc />
		public static bool operator >=(MetonIdentifier left, MetonIdentifier right) => left.CompareTo(right) >= 0;
	}
}
