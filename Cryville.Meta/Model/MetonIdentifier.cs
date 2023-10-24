using System;
using System.IO;

namespace Cryville.Meta.Model {
	public struct MetonIdentifier : IModel, IComparable<MetonIdentifier> {
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
	}
}
