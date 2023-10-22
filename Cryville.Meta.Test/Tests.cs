using NUnit.Framework;
using System.IO;

namespace Cryville.Meta.Test {
	public class Tests {
		CmdbConnection _db;

		[SetUp]
		public void SetUp() {
			_db = new(Path.Combine(TestContext.CurrentContext.WorkDirectory, "tests.cmdb"));
		}
		[TearDown]
		public void TearDown() {
			_db.Dispose();
		}
		[Test]
		public void OpenConnection() {
			Assert.Pass();
		}
	}
}
