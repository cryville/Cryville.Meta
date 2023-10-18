using System.IO;

namespace Cryville.Meta.Model {
	internal struct CmdbDynamicHeader : IModel {
		public uint FileChangeCounter;
		public uint PageCount;
		public const int Reserved = 0x78;

		public void ReadFrom(BinaryReader reader) {
			FileChangeCounter = reader.ReadUInt32();
			PageCount = reader.ReadUInt32();
		}
		public void WriteTo(BinaryWriter writer) {
			writer.Write(FileChangeCounter);
			writer.Write(PageCount);
		}
	}
}
