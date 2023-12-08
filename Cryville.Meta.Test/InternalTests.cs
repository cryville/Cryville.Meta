using Cryville.Meta.Model;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Cryville.Meta.Test {
	public class InternalTests {
		static readonly string TestPath = Path.Combine(Environment.CurrentDirectory, "internal_tests.cmdb");
		CmdbConnection _db;
		readonly Stopwatch _stopwatch = new();

		static readonly MetonPairModel _samplePair = new() {
			KeyPointer = 233,
			ValuePointer = 2333,
			Key = new MetonIdentifier { TypeKey = 0xd000_0000_0000_0000, SubKey1 = 0x0123_4567_89ab_cdef },
			Value = new MetonIdentifier { TypeKey = 0x0000_0000_0000_0000, SubKey1 = 0xfedc_ba98_7654_3210 },
		};

		[SetUp]
		public void SetUp() {
			TestContext.WriteLine("Name: {0}", TestContext.CurrentContext.Test.FullName);
#if NETFRAMEWORK4_5_OR_GREATER
			TestContext.WriteLine("Framework: {0}", AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName);
#elif NETFRAMEWORK
			TestContext.WriteLine("Framework: .NETFramework,Version=v{0}", Environment.Version);
#else
			TestContext.WriteLine("Framework: {0}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
#endif
		}

		void TestVerify(Action write, Action read) {
			if (File.Exists(TestPath)) File.Delete(TestPath);
			using (_db = new CmdbConnection(TestPath)) {
				_stopwatch.Reset();
				_stopwatch.Start();
				write?.Invoke();
				TestContext.WriteLine("Write time: {0:F3} ms", (double)_stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000);
			}
			using (_db = new CmdbConnection(TestPath)) {
				_stopwatch.Reset();
				_stopwatch.Start();
				read?.Invoke();
				TestContext.WriteLine("Read time: {0:F3} ms", (double)_stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000);
			}
		}

		[Test]
		public void Pager() {
			TestVerify(
				() => {
					CmdbConnection.BlockScope block1, block2;
					using (block1 = _db.AcquireFreeBlock(128)) {
						_db.Writer.Write(new byte[128]);
					}
					using (_db.AcquireFreeBlock(129)) {
						_db.Writer.Write(new byte[129]);
					}
					using (block2 = _db.AcquireFreeBlock(128)) {
						_db.Writer.Write(new byte[128]);
					}
					using (_db.AcquireFreeBlock(129)) {
						_db.Writer.Write(new byte[129]);
					}
					_db.ReleaseBlock(block2.Pointer, 128);
					_db.ReleaseBlock(block1.Pointer, 128);
				},
				null
			);
		}

		[Test]
		public void Insert() {
			TestVerify(
				() => {
					var node = BTreeNode.Create(_db, null);
					node.Insert(0, _samplePair);
					node.Insert(1, _samplePair);
					node.Insert(0, _samplePair);
				},
				() => {
					var node = new BTreeNode(_db, 2 * (ulong)_db.PageSize);
					Assert.That(node.Count, Is.EqualTo(3));
					Assert.Multiple(() => {
						Assert.That(node.GetMetonPair(0), Is.EqualTo(_samplePair));
						Assert.That(node.GetMetonPair(1), Is.EqualTo(_samplePair));
						Assert.That(node.GetMetonPair(2), Is.EqualTo(_samplePair));
					});
				}
			);
		}

		[Test]
		[SuppressMessage("Assertion", "NUnit2045")]
		public void CursorSearch() {
			TestVerify(
				() => {
					var pair = _samplePair;
					var node = BTreeNode.Create(_db, null);
					node.Insert(0, pair);
					pair.Key.TypeKey = 0xd000_0000_0000_0001;
					node.Insert(1, pair);
					pair.Key.TypeKey = 0xd000_0000_0000_0003;
					node.Insert(2, pair);
				},
				() => {
					var pair = _samplePair;
					var set = new MetonPairSet(_db, (ulong)_db.PageSize) {
						RootNode = new BTreeNode(_db, 2 * (ulong)_db.PageSize)
					};
					using (var cursor = new BTreeCursor(set)) {
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current.Key.TypeKey, Is.EqualTo(0xd000_0000_0000_0000));
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current.Key.TypeKey, Is.EqualTo(0xd000_0000_0000_0001));
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current.Key.TypeKey, Is.EqualTo(0xd000_0000_0000_0003));
						Assert.That(!cursor.MoveNext());
					}
					using (var cursor = new BTreeCursor(set)) {
						pair.Key.TypeKey = 0xd000_0000_0000_0001;
						Assert.That(cursor.Search(pair));
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current.Key.TypeKey, Is.EqualTo(0xd000_0000_0000_0001));
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current.Key.TypeKey, Is.EqualTo(0xd000_0000_0000_0003));
						Assert.That(!cursor.MoveNext());
					}
					using (var cursor = new BTreeCursor(set)) {
						pair.Key.TypeKey = 0xd000_0000_0000_0002;
						Assert.That(!cursor.Search(pair));
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current.Key.TypeKey, Is.EqualTo(0xd000_0000_0000_0003));
						Assert.That(!cursor.MoveNext());
					}
					using (var cursor = new BTreeCursor(set)) {
						pair.Key.TypeKey = 0xd000_0000_0000_0004;
						Assert.That(!cursor.Search(pair));
						Assert.That(!cursor.MoveNext());
					}
				}
			);
		}

		[Test]
		[SuppressMessage("Assertion", "NUnit2045")]
		public void CursorAdd() {
			TestVerify(
				() => {
					var pair = _samplePair;
					using (var block = _db.AcquireFreeBlock(_db.PageSize)) {
						_db.Writer.Write((ulong)0);
						_db.SeekCurrent(_db.PageSize - 8);
					}
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					var cursor = new BTreeCursor(set);
					Assert.That(cursor.Add(pair));
					Assert.That(!cursor.Add(pair));
					for (var i = 0; i < (_db.BTreeOrder + 2) * _db.BTreeOrder; i++) {
						pair.Key.TypeKey++;
						Assert.That(cursor.Add(pair));
					}
				},
				() => {
					var pair = _samplePair;
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					_ = set.Count; // Initialize the set
					var cursor = new BTreeCursor(set);
					for (var i = 0; i < (_db.BTreeOrder + 2) * _db.BTreeOrder + 1; i++) {
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current, Is.EqualTo(pair));
						pair.Key.TypeKey++;
					}
					Assert.That(!cursor.MoveNext());
				}
			);
		}
	}
}
