using NUnit.Framework;
using System.IO;

namespace Cryville.Meta.Test {
	public class CreationTest {
		[Test]
		public void Test() {
			var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "creation_test.cmdb");
			if (File.Exists(path)) File.Delete(path);
			using var db = new CmdbConnection(path);
		}
	}
}
