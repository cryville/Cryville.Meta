using System.IO;

namespace Cryville.Meta {
	internal class CmdbReader : BinaryReader {
		public CmdbReader(Stream input) : base(input, Shared.Encoding) { }

		protected override void Dispose(bool disposing) {
			// Do nothing
		}
		public override void Close() {
			Dispose(true);
		}
	}
}