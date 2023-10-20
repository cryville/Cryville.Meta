using System.IO;

namespace Cryville.Meta.Model {
	internal struct MetonModel : IModel {
		public uint RootMetonPairPageIndex;
		public uint SummaryLength;

		public void ReadFrom(BinaryReader reader) {
			RootMetonPairPageIndex = reader.ReadUInt32();
			SummaryLength = reader.ReadUInt32();
		}
		public readonly void WriteTo(BinaryWriter writer) {
			writer.Write(RootMetonPairPageIndex);
			writer.Write(SummaryLength);
		}
	}
}
