using Cryville.Meta.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Cryville.Meta {
	internal class BTreeNode : IBTreeNodeParent, IDisposable {
		readonly CmdbConnection _db;
		readonly MetonPairSet _set;
		public ulong NodePointer { get; private set; }
		BTreeNode _parent;

		public bool IsRoot {
			get {
				CheckDisposed();
				return _parent == null;
			}
		}
		public bool IsLeaf {
			get {
				CheckDisposed();
				return _childPtrs[0] == 0;
			}
		}

		List<short> _cellIndices;
		List<ulong> _childPtrs;
		Queue<int> _freeCells;
		MetonPairModel[] _metonPairs;
		List<BTreeNode> _children;

		internal int Version;

		int m_totalCount;
		public int TotalCount {
			get {
				CheckDisposed();
				LazyInit0();
				return m_totalCount;
			}
		}
		int m_count;
		public int Count {
			get {
				CheckDisposed();
				LazyInit1();
				return m_count;
			}
		}

		// TODO combine set and parent with interface
		public BTreeNode(CmdbConnection db, MetonPairSet set, ulong nodePtr) : this(db, set, nodePtr, null) { }
		BTreeNode(CmdbConnection db, MetonPairSet set, ulong nodePtr, BTreeNode parent) {
			_db = db;
			_set = set;
			NodePointer = nodePtr;
			_parent = parent;

			Debug.Assert(NodePointer > 0 && NodePointer % (ulong)_db.PageSize == 0);
		}
		int m_isDisposed;
		public bool IsDisposed => m_isDisposed > 0;
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing) {
			if (Interlocked.Exchange(ref m_isDisposed, 1) > 0) return;
			if (disposing) {
				foreach (var child in _children) {
					if (child == null) continue;
					child.Dispose();
				}
			}
		}
		void CheckDisposed() {
			if (IsDisposed) throw new ObjectDisposedException(null);
		}

		bool _init0;
		void LazyInit0() {
			if (_init0) return;
			_init0 = true;

			_db.Seek((long)NodePointer);
			m_totalCount = (int)_db.Reader.ReadUInt64();
		}
		bool _init1;
		void LazyInit1() {
			if (_init1) return;
			_init1 = true;

			_cellIndices = new(_db.BTreeOrder);
			_childPtrs = new(_db.BTreeOrder + 1);
			_freeCells = new(_db.BTreeOrder);
			_metonPairs = new MetonPairModel[_db.BTreeOrder];
			_children = new(_db.BTreeOrder + 1);
			var usedCells = new HashSet<int>();

			_db.Seek((long)NodePointer + 0x08);
			m_count = 0;
			for (; m_count < _db.BTreeOrder; m_count++) {
				var ptr = _db.Reader.ReadUInt64();
				_childPtrs.Add(ptr);
				_children.Add(null);

				var index = _db.Reader.ReadInt16();
				if (index < 0) break;
				_cellIndices.Add(index);
				usedCells.Add(index);
			}
			if (m_count == _db.BTreeOrder) {
				_childPtrs.Add(_db.Reader.ReadUInt64());
				_children.Add(null);
			}

			Debug.Assert(_cellIndices.Count <= _db.BTreeOrder);
			Debug.Assert(_childPtrs.Count <= _db.BTreeOrder + 1);

			for (var i = 0; i < _db.BTreeOrder; i++) {
				if (!usedCells.Contains(i)) _freeCells.Enqueue(i);
			}
		}

		public MetonPairModel GetMetonPair(int index) {
			CheckDisposed();
			LazyInit1();
			if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
			var cellIndex = _cellIndices[index];
			var pair = _metonPairs[cellIndex];
			if (pair.KeyPointer == 0) _metonPairs[cellIndex] = pair = _db.Reader.ReadModel<MetonPairModel>();
			return pair;
		}
		public BTreeNode GetChildNode(int index) {
			CheckDisposed();
			LazyInit1();
			if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
			var ret = _children[index];
			if (ret == null) _children[index] = ret = new(_db, _set, _childPtrs[index], this);
			return ret;
		}
		public int BinarySearch(MetonPairModel value, out MetonPairModel result) {
			int l = 0;
			int r = Count - 1;
			while (l <= r) {
				int m = l + (r - l >> 1);
				var v = GetMetonPair(m);
				int cmp = v.CompareTo(value);
				if (cmp == 0) {
					result = v;
					return m;
				}
				if (cmp < 0) l = m + 1;
				else r = m - 1;
			}
			result = default;
			return ~l;
		}
	}
}
