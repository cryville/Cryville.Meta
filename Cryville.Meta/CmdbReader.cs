using System.IO;

namespace Cryville.Meta {
	internal class CmdbReader(Stream input) : BinaryReader(input, Shared.Encoding) {
		protected override void Dispose(bool disposing) {
			// Do nothing
		}
		public override void Close() {
			Dispose(true);
		}
	}
}