using System;
using System.IO;

namespace Cryville.Meta.Model {
	internal struct MetonPairModel : IModel, IComparable<MetonPairModel> {
		public ulong KeyPointer;
		public MetonIdentifier Key;
		public ulong ValuePointer;
		public MetonIdentifier Value;

		public readonly int CompareTo(MetonPairModel other) {
			int r = Key.CompareTo(other.Key);
			if (r != 0) return r;
			return Value.CompareTo(other.Value);
		}

		public void ReadFrom(BinaryReader reader) {
			KeyPointer = reader.ReadUInt64();
			Key = reader.ReadModel<MetonIdentifier>();
			ValuePointer = reader.ReadUInt64();
			Value = reader.ReadModel<MetonIdentifier>();
		}
		public readonly void WriteTo(BinaryWriter writer) {
			writer.Write(KeyPointer);
			writer.Write(Key);
			writer.Write(ValuePointer);
			writer.Write(Value);
		}
	}
}
