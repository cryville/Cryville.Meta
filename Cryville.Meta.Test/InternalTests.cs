using Cryville.Meta.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

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
				LogTime("Write");
			}
			using (_db = new CmdbConnection(TestPath)) {
				_stopwatch.Reset();
				_stopwatch.Start();
				read?.Invoke();
				LogTime("Read");
			}
		}

		void LogTime(string prefix) {
			TestContext.WriteLine("{0}: {1:F3} ms", prefix, (double)_stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000);
			_stopwatch.Reset();
			_stopwatch.Start();
		}

		[Test]
		public void Pager() {
			TestVerify(
				() => {
					CmdbConnection.BlockScope block1, block2, block3;
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
					using (block3 = _db.AcquireFreeBlock(_db.PageSize)) {
						_db.Writer.Write(new byte[_db.PageSize]);
					}
					_db.ReleaseBlock(block2.Pointer, 128);
					_db.ReleaseBlock(block1.Pointer, 128);
					_db.ReleaseBlock(block3.Pointer, _db.PageSize);
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

		[Test]
		[SuppressMessage("Assertion", "NUnit2045")]
		public void CursorAddRandom() {
			var random = new Random();
			IEnumerable<MetonPairModel> pairs = null;
			TestVerify(
				() => {
					pairs = Enumerable.Repeat(0, (_db.BTreeOrder + 2) * _db.BTreeOrder + 1).Select(
						i => new MetonPairModel {
							KeyPointer = 233,
							ValuePointer = 2333,
							Key = new MetonIdentifier { TypeKey = 0xd000_0000_0000_0000, SubKey1 = (ulong)random.Next() },
							Value = new MetonIdentifier { TypeKey = 0x0000_0000_0000_0000, SubKey1 = 0xfedc_ba98_7654_3210 },
						}
					).Distinct().ToArray();
					TestContext.WriteLine("Count: {0}", pairs.Count());
					LogTime("Generate pairs");
					using (var block = _db.AcquireFreeBlock(_db.PageSize)) {
						_db.Writer.Write((ulong)0);
						_db.SeekCurrent(_db.PageSize - 8);
					}
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					var cursor = new BTreeCursor(set);
					foreach (var pair in pairs) {
						Assert.That(cursor.Add(pair));
					}
				},
				() => {
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					_ = set.Count; // Initialize the set
					var cursor = new BTreeCursor(set);
					foreach (var pair in pairs.OrderBy(i => i)) {
						Assert.That(cursor.MoveNext());
						Assert.That(cursor.Current, Is.EqualTo(pair));
					}
					Assert.That(!cursor.MoveNext());
				}
			);
		}

		[Test]
		[SuppressMessage("Assertion", "NUnit2045")]
		public void CursorRemove() {
			TestVerify(
				() => {
					var pair = _samplePair;
					using (var block = _db.AcquireFreeBlock(_db.PageSize)) {
						_db.Writer.Write((ulong)0);
						_db.SeekCurrent(_db.PageSize - 8);
					}
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					var cursor = new BTreeCursor(set);
					for (var i = 0; i < (_db.BTreeOrder + 2) * _db.BTreeOrder + 1; i++) {
						pair.Key.TypeKey++;
						Assert.That(cursor.Add(pair));
					}
					pair = _samplePair;
					for (var i = 0; i < (_db.BTreeOrder + 2) * _db.BTreeOrder + 1; i++) {
						pair.Key.TypeKey++;
						Assert.That(cursor.Remove(pair));
						Assert.That(!cursor.Remove(pair));
					}
				},
				() => {
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					_ = set.Count; // Initialize the set
					using var cursor = new BTreeCursor(set);
					Assert.That(!cursor.MoveNext());
				}
			);
		}

		[Test]
		public void CursorRemoveRandom() {
			var random = new Random();
			IEnumerable<MetonPairModel> pairs = null;
			TestVerify(
				() => {
					pairs = Enumerable.Repeat(0, (_db.BTreeOrder + 2) * _db.BTreeOrder + 1).Select(
						i => new MetonPairModel {
							KeyPointer = 233,
							ValuePointer = 2333,
							Key = new MetonIdentifier { TypeKey = 0xd000_0000_0000_0000, SubKey1 = (ulong)random.Next() },
							Value = new MetonIdentifier { TypeKey = 0x0000_0000_0000_0000, SubKey1 = 0xfedc_ba98_7654_3210 },
						}
					).Distinct().ToArray();
					TestContext.WriteLine("Count: {0}", pairs.Count());
					LogTime("Generate pairs");
					using (var block = _db.AcquireFreeBlock(_db.PageSize)) {
						_db.Writer.Write((ulong)0);
						_db.SeekCurrent(_db.PageSize - 8);
					}
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					var cursor = new BTreeCursor(set);
					foreach (var pair in pairs) {
						Assert.That(cursor.Add(pair));
					}
				},
				() => {
					var set = new MetonPairSet(_db, 2 * (ulong)_db.PageSize);
					_ = set.Count; // Initialize the set
					var cursor = new BTreeCursor(set);
					foreach (var pair in pairs.OrderBy(i => random.Next())) {
						Assert.That(cursor.Remove(pair));
					}
					cursor.Reset();
					Assert.That(!cursor.MoveNext());
				}
			);
		}
	}
}
