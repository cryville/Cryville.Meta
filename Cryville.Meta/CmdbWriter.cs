using System.IO;

namespace Cryville.Meta {
	internal class CmdbWriter(Stream output) : BinaryWriter(output, Shared.Encoding) {
		protected override void Dispose(bool disposing) {
			// Do nothing
		}
		public override void Close() {
			Dispose(true);
		}
	}
}
