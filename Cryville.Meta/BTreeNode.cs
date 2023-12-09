using Cryville.Meta.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Cryville.Meta {
	internal class BTreeNode : IDisposable {
		#region Data structure
		readonly CmdbConnection _db;
		public ulong NodePointer { get; private set; }

		public bool IsLeaf {
			get {
				CheckDisposed();
				return LazyData._childPtrs[0] == 0;
			}
		}

		internal int Version;

		class LazyData_1 {
#pragma warning disable CS8618
			public int m_count;
			public List<short> _cellIndices;
			public List<ulong> _childPtrs;
			public Queue<short> _freeCells;
			public MetonPairModel[] _metonPairs;
			public List<BTreeNode?> _children;
#pragma warning restore CS8618
		}
		LazyData_1? _lazyData;

		int m_totalCount;
		public int TotalCount {
			get {
				CheckDisposed();
				LazyInit();
				return m_totalCount;
			}
		}
		public int Count {
			get {
				CheckDisposed();
				return LazyData.m_count;
			}
		}
		public bool IsFull => Count == _db.BTreeOrder;
		#endregion

		#region Lifecycle
		public BTreeNode(CmdbConnection db, ulong nodePtr) {
			_db = db;
			NodePointer = nodePtr;

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
				if (LazyData is LazyData_1 data) {
					foreach (var child in data._children) {
						child?.Dispose();
					}
				}
			}
		}
		void CheckDisposed() {
			if (IsDisposed) throw new ObjectDisposedException(null);
		}

		bool _init;
		void LazyInit() {
			if (_init) return;
			_init = true;

			_db.Seek((long)NodePointer);
			m_totalCount = (int)_db.Reader.ReadUInt64();
		}
		LazyData_1 LazyData {
			get {
				if (_lazyData is LazyData_1 data)
					return data;
				_lazyData = data = new LazyData_1();
				LazyInit();

				data._cellIndices = new(_db.BTreeOrder);
				data._childPtrs = new(_db.BTreeOrder + 1);
				data._freeCells = new(_db.BTreeOrder);
				data._metonPairs = new MetonPairModel[_db.BTreeOrder];
				data._children = new(_db.BTreeOrder + 1);
				var usedCells = new HashSet<int>();

				_db.Seek((long)NodePointer + 0x08);
				data.m_count = 0;
				for (; data.m_count < _db.BTreeOrder; data.m_count++) {
					var ptr = _db.Reader.ReadUInt64();
					data._childPtrs.Add(ptr);
					data._children.Add(null);

					var cellIndex = _db.Reader.ReadInt16();
					if (cellIndex < 0) break;
					Debug.Assert(cellIndex < _db.BTreeOrder);
					data._cellIndices.Add(cellIndex);
					usedCells.Add(cellIndex);
				}
				if (data.m_count == _db.BTreeOrder) {
					var ptr = _db.Reader.ReadUInt64();
					data._childPtrs.Add(ptr);
					data._children.Add(null);
				}

				Debug.Assert(data._cellIndices.Count <= _db.BTreeOrder);
				Debug.Assert(data._childPtrs.Count <= _db.BTreeOrder + 1);

				for (short i = 0; i < _db.BTreeOrder; i++) {
					if (!usedCells.Contains(i)) data._freeCells.Enqueue(i);
				}

				return data;
			}
		}
		#endregion

		#region Seek methods
		void SeekToChildPointer(int index) => _db.Seek((long)NodePointer + 8 + index * 10);
		void SeekToCellIndex(int index) => _db.Seek((long)NodePointer + 8 + index * 10 + 8);
		void SeekToCell(int index) => _db.Seek((long)NodePointer + _db.BTreeContentAreaOffset + index * 96);
		#endregion

		#region Read-only methods
		public MetonPairModel GetMetonPair(int index) {
			CheckDisposed();
			var data = LazyData;
			Debug.Assert(index >= 0 || index < Count);
			var cellIndex = data._cellIndices[index];
			var pair = data._metonPairs[cellIndex];
			if (pair.KeyPointer == 0) {
				SeekToCell(cellIndex);
				data._metonPairs[cellIndex] = pair = _db.Reader.ReadModel<MetonPairModel>();
			}
			return pair;
		}
		public BTreeNode GetChildNode(int index) {
			CheckDisposed();
			var data = LazyData;
			Debug.Assert(!IsLeaf);
			Debug.Assert(index >= 0 || index <= Count);
			var ret = data._children[index];
			if (ret == null) data._children[index] = ret = new(_db, data._childPtrs[index]);
			return ret;
		}
		public int BinarySearch(MetonPairModel value, out MetonPairModel result) {
			CheckDisposed();
			_ = LazyData;
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
		#endregion

		#region Write methods
		/// <summary>
		/// Writes the cell indices into the database file.
		/// </summary>
		/// <param name="index">The start index where the cell indices are to be written.</param>
		internal void WriteCellIndices(int index) {
			var data = LazyData;
			Debug.Assert(index <= data.m_count);
			SeekToCellIndex(index);
			for (var i = index; i < data.m_count;) {
				_db.Writer.Write(data._cellIndices[i]);
				_db.Writer.Write(data._childPtrs[++i]);
			}
			if (data.m_count < _db.BTreeOrder) _db.Writer.Write((short)-1);
			Version++;
		}
		/// <summary>
		/// Inserts a meton pair as a child of this node.
		/// </summary>
		/// <param name="index">The index where the child is to be inserted.</param>
		/// <param name="value">The meton pair to be inserted.</param>
		/// <param name="child">The child node on the right of the meton pair.</param>
		internal void InsertInternal(int index, MetonPairModel value, BTreeNode? child) {
			var data = LazyData;

			Debug.Assert(value.Key.PairType != MetonPairType.None);
			Debug.Assert(value.KeyPointer != 0);
			Debug.Assert(value.Value.PairType == MetonPairType.None);
			Debug.Assert(value.ValuePointer != 0);

			var cellIndex = data._freeCells.Dequeue();
			data._cellIndices.Insert(index, cellIndex);
			data._metonPairs[cellIndex] = value;

			index++;
			Debug.Assert(child == null == IsLeaf);
			if (child == null) {
				data._childPtrs.Add(0);
				data._children.Add(null);
			}
			else {
				data._childPtrs.Insert(index, child.NodePointer);
				data._children.Insert(index, child);
			}
			data.m_count++;

			SeekToCell(cellIndex);
			_db.Writer.Write(value);
		}
		/// <summary>
		/// Removes a meton pair at the specified index from this node.
		/// </summary>
		/// <param name="index">The index where the child to be removed is.</param>
		/// <param name="value">The removed meton pair.</param>
		/// <param name="child">The child node on the right of the removed meton pair.</param>
		void RemoveInternal(int index, out MetonPairModel value, out BTreeNode? child) {
			var data = LazyData;

			--data.m_count;
			var cellIndex = data._cellIndices[index];
			value = data._metonPairs[cellIndex];
			data._freeCells.Enqueue(cellIndex);
			data._cellIndices.RemoveAt(index);

			index++;
			child = data._children[index];
			data._childPtrs.RemoveAt(index);
			data._children.RemoveAt(index);

		}
		public void Insert(int index, MetonPairModel value) {
			CheckDisposed();
			var data = LazyData;
			Debug.Assert(IsLeaf);
			Debug.Assert(!IsFull);
			Debug.Assert(index >= 0 && index <= data.m_count);

			InsertInternal(index, value, null);
			WriteCellIndices(index);
		}
		public void Remove(int index) {
			CheckDisposed();
			var data = LazyData;
			Debug.Assert(IsLeaf);
			Debug.Assert(data.m_count > 0);
			Debug.Assert(index >= 0 && index < data.m_count);

			RemoveInternal(index, out _, out _);
			WriteCellIndices(index);
		}

		internal static BTreeNode Create(CmdbConnection db, BTreeNode? firstChild) {
			var firstChildPtr = firstChild?.NodePointer ?? 0UL;
			using var block = db.AcquireFreeBlock(db.BTreeSize);
			LazyData_1 data;
			var ret = new BTreeNode(db, block.Pointer) {
				_init = true,
				_lazyData = data = new() {
					_cellIndices = new(db.BTreeOrder),
					_childPtrs = new(db.BTreeOrder + 1) { firstChildPtr },
					_freeCells = new(db.BTreeOrder),
					_metonPairs = new MetonPairModel[db.BTreeOrder],
					_children = new(db.BTreeOrder + 1) { firstChild },
					m_count = 0,
				}
			};
			for (short i = 0; i < db.BTreeOrder; i++) {
				data._freeCells.Enqueue(i);
			}
			db.Writer.Write(0UL);
			db.Writer.Write(firstChildPtr);
			db.Writer.Write((short)-1);
			db.SeekCurrent(db.BTreeSize - 0x12);
			return ret;
		}
		void Release() {
			var data = LazyData;
			Debug.Assert(data.m_count == 0);
			// TODO Set parent pointer
			_db.ReleaseBlock(NodePointer, _db.BTreeSize);
			Dispose();
		}

		public bool SplitInsert(int index, ref MetonPairModel carry, ref BTreeNode? carryChild, int carryIndex) {
			CheckDisposed();
			var data = LazyData;
			Debug.Assert(!IsLeaf);

			SplitInsert(_db, GetChildNode(index), ref carry, ref carryChild, carryIndex);

			if (data.m_count < _db.BTreeOrder) {
				// If there are free slots, insert the right node
				InsertInternal(index, carry, carryChild);
				WriteCellIndices(index);
				return true;
			}
			else {
				// Otherwise, pass the carried meton pair and child to the cursor
				return false;
			}
		}

		internal static void SplitInsert(CmdbConnection db, BTreeNode splitNode, ref MetonPairModel carry, ref BTreeNode? carryChild, int carryIndex) {
			Debug.Assert(splitNode.IsFull);
			var leftCount = db.BTreeOrder / 2;
			var rightCount = db.BTreeOrder - leftCount;

			if (carryIndex != leftCount) {
				var oldCarry = carry;
				var oldCarryChild = carryChild;

				// Split at index other than carryIndex
				var splitIndex = leftCount;
				if (carryIndex < leftCount) --splitIndex;
				else --carryIndex;

				splitNode.RemoveInternal(splitIndex, out carry, out carryChild);
				splitNode.InsertInternal(carryIndex, oldCarry, oldCarryChild);
			}

			// The reassigned carryChild is the right node here
			carryChild = Create(db, carryChild);

			for (var i = 0; i < rightCount; i++) {
				splitNode.RemoveInternal(leftCount, out var pair, out var child);
				carryChild.InsertInternal(i, pair, child);
			}

			splitNode.WriteCellIndices(Math.Min(leftCount, carryIndex));
			carryChild.WriteCellIndices(0);
		}
		#endregion
	}
}
