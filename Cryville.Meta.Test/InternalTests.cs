using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace Cryville.Meta.Test {
	public class InternalTests {
		static readonly string TestPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "internal_tests.cmdb");
		CmdbConnection _db;
		readonly Stopwatch _stopwatch = new();
		[SetUp]
		public void SetUp() {
			if (File.Exists(TestPath)) File.Delete(TestPath);
			_db = new CmdbConnection(TestPath);
			_stopwatch.Reset();
			_stopwatch.Start();
		}
		[TearDown]
		public void TearDown() {
			TestContext.WriteLine("Body time: {0} ms", _stopwatch.ElapsedMilliseconds);
			_db.Dispose();
			using var db = new CmdbConnection(TestPath);
		}

		[Test]
		public void Pager() {
			using (_db.AcquireFreeBlock(512)) {
				_db.Writer.Write(new byte[512]);
			}
			using (_db.AcquireFreeBlock(513)) {
				_db.Writer.Write(new byte[513]);
			}
			using (_db.AcquireFreeBlock(512)) {
				_db.Writer.Write(new byte[512]);
			}
			using (_db.AcquireFreeBlock(513)) {
				_db.Writer.Write(new byte[513]);
			}
			_db.ReleaseBlock(2 * (ulong)_db.PageSize + 512 + 520, 512);
			_db.ReleaseBlock(2 * (ulong)_db.PageSize, 512);
		}
	}
}
