using System;
using System.IO;

namespace Cryville.Meta.Model {
	public struct MetonIdentifier : IModel, IComparable<MetonIdentifier> {
		public long TypeKey { get; set; }
		public ulong SubKey1 { get; set; }
		public ulong SubKey2 { get; set; }
		public ulong SubKey3 { get; set; }
		public ulong SubKey4 { get; set; }

		internal bool IsBackward => TypeKey < 0;

		public int CompareTo(MetonIdentifier other) {
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
			TypeKey = reader.ReadInt64();
			SubKey1 = reader.ReadUInt64();
			SubKey2 = reader.ReadUInt64();
			SubKey3 = reader.ReadUInt64();
			SubKey4 = reader.ReadUInt64();
		}
		void IModel.WriteTo(BinaryWriter writer) {
			writer.Write(TypeKey);
			writer.Write(SubKey1);
			writer.Write(SubKey2);
			writer.Write(SubKey3);
			writer.Write(SubKey4);
		}
	}
}
