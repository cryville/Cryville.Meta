using System.IO;

namespace Cryville.Meta.Model {
	internal struct CmdbStaticHeader : IModel {
		public ulong Magic;
		public uint Version;
		public byte PageSize;
		public const int Reserved = 0x73;

		public void ReadFrom(BinaryReader reader) {
			Magic = reader.ReadUInt64();
			Version = reader.ReadUInt32();
			PageSize = reader.ReadByte();
		}
		public void WriteTo(BinaryWriter writer) {
			writer.Write(Magic);
			writer.Write(Version);
			writer.Write(PageSize);
		}
	}
}
