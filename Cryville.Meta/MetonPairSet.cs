using Cryville.Meta.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Cryville.Meta {
	public class MetonPairSet : ICollection<MetonPair>, IEnumerable<MetonPair>, ICollection, IEnumerable, IDisposable {
		readonly CmdbConnection _db;
		readonly ulong _ptr;
		ulong _nodePointer;
		internal BTreeNode? RootNode;

		#region Lifecycle
		internal MetonPairSet(CmdbConnection db, ulong ptr) {
			_db = db;
			_ptr = ptr;
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
				_db.OnMetonPairSetDispose(_ptr);
				RootNode?.Dispose();
			}
		}
		void CheckDisposed() {
			if (IsDisposed) throw new ObjectDisposedException(null);
		}

		bool _init;
		void LazyInit() {
			if (_init) return;
			_init = true;
			_db.Seek((long)_ptr);
			_nodePointer = _db.Reader.ReadUInt64();
			if (_nodePointer != 0) {
				RootNode = new(_db, _nodePointer);
			}
		}
		#endregion

		public int Count {
			get {
				LazyInit();
				return RootNode?.TotalCount ?? 0;
			}
		}

		public bool IsReadOnly => _db.IsReadOnly;
		public object SyncRoot => this;
		public bool IsSynchronized => false;

		internal void CreateRootNode(MetonPairModel value) {
			CheckDisposed();
			LazyInit();

			Debug.Assert(RootNode == null);
			RootNode = BTreeNode.Create(_db, null);
			RootNode.Insert(0, value);
			_db.Seek((long)_ptr);
			_db.Writer.Write(_nodePointer = RootNode.NodePointer);
		}
		internal void ReleaseRootNode() {
			CheckDisposed();
			LazyInit();

			Debug.Assert(RootNode != null);
			Debug.Assert(RootNode.Count == 0);
			RootNode = RootNode.Release();
			_db.Seek((long)_ptr);
			_db.Writer.Write(_nodePointer = RootNode?.NodePointer ?? 0);
		}

		[SuppressMessage("Reliability", "CA2000")]
		internal void SplitInsert(MetonPairModel carry, BTreeNode? carryChild, int carryIndex) {
			CheckDisposed();
			LazyInit();

			Debug.Assert(RootNode != null);
			BTreeNode.SplitInsert(_db, RootNode, ref carry, ref carryChild, carryIndex);

			RootNode = BTreeNode.Create(_db, RootNode);
			RootNode.InsertInternal(0, carry, carryChild);
			RootNode.WriteCellIndices(0);
			_db.Seek((long)_ptr);
			_db.Writer.Write(_nodePointer = RootNode.NodePointer);
		}

		public void Add(MetonPair item) {
			CheckDisposed();
			if (IsReadOnly) throw new NotSupportedException("The connection is read-only.");
			LazyInit();
			throw new NotImplementedException();
		}

		public void Clear() {
			CheckDisposed();
			if (IsReadOnly) throw new NotSupportedException("The connection is read-only.");
			LazyInit();
			if (RootNode == null) return;
			throw new NotImplementedException();
		}

		public bool Contains(MetonPair item) {
			CheckDisposed();
			if (RootNode == null) return false;
			throw new NotImplementedException();
		}

		public void CopyTo(MetonPair[] array, int arrayIndex) {
			CheckDisposed();
			throw new NotImplementedException();
		}

		public void CopyTo(Array array, int index) {
			CheckDisposed();
			throw new NotImplementedException();
		}

		public bool Remove(MetonPair item) {
			CheckDisposed();
			if (IsReadOnly) throw new NotSupportedException("The connection is read-only.");
			LazyInit();
			if (RootNode == null) return false;
			throw new NotImplementedException();
		}

		public Enumerator GetEnumerator() {
			CheckDisposed();
			LazyInit();
			return new(this);
		}
		IEnumerator<MetonPair> IEnumerable<MetonPair>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Enumerator : IEnumerator<MetonPair>, IEnumerator {
			internal Enumerator(MetonPairSet self) { }

			public readonly MetonPair Current => throw new NotImplementedException();
			readonly object IEnumerator.Current => Current;

			public readonly void Dispose() { }

			public bool MoveNext() {
				throw new NotImplementedException();
			}

			public void Reset() {
				throw new NotImplementedException();
			}
		}
	}
}
