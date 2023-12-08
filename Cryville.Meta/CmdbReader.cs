using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Cryville.Meta {
	internal class CmdbReader(Stream input) : BinaryReader(input, Shared.Encoding) {
		[SuppressMessage("Usage", "CA2215", Justification = "Leave open")]
		protected override void Dispose(bool disposing) {
			// Do nothing
		}
		public override void Close() {
			Dispose(true);
		}
	}
}