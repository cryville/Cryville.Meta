using System;
using System.IO;

namespace Cryville.Meta.Model {
	internal struct MetonPair : IModel, IComparable<MetonPair> {
		public MetonIdentifier Key;
		public ulong KeyPointer;
		public MetonIdentifier Value;
		public ulong ValuePointer;

		public int CompareTo(MetonPair other) {
			int r = Key.CompareTo(other.Key);
			if (r != 0) return r;
			return Value.CompareTo(other.Value);
		}

		public void ReadFrom(BinaryReader reader) {
			Key = reader.ReadModel<MetonIdentifier>();
			KeyPointer = reader.ReadUInt64();
			Value = reader.ReadModel<MetonIdentifier>();
			ValuePointer = reader.ReadUInt64();
		}
		public void WriteTo(BinaryWriter writer) {
			writer.Write(Key);
			writer.Write(KeyPointer);
			writer.Write(Value);
			writer.Write(ValuePointer);
		}
	}
}
