using System.IO;

namespace Cryville.Meta {
	internal class CmdbWriter : BinaryWriter {
		public CmdbWriter(Stream output) : base(output, Shared.Encoding) { }

		protected override void Dispose(bool disposing) {
			// Do nothing
		}
		public override void Close() {
			Dispose(true);
		}
	}
}
