using System.IO;

namespace Cryville.Meta.Model {
	internal struct MetonModel : IModel {
		public ulong RootNodePointer;
		public ulong SummaryLength;

		public void ReadFrom(BinaryReader reader) {
			RootNodePointer = reader.ReadUInt64();
			SummaryLength = reader.ReadUInt64();
		}
		public readonly void WriteTo(BinaryWriter writer) {
			writer.Write(RootNodePointer);
			writer.Write(SummaryLength);
		}
	}
}
