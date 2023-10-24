using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Cryville.Meta {
	public class MetonPairSet : IBTreeNodeParent, ICollection<MetonPair>, IEnumerable<MetonPair>, ICollection, IEnumerable, IDisposable {
		readonly CmdbConnection _db;
		readonly ulong _ptr;
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
		internal BTreeNode RootNode;
		void LazyInit() {
			if (_init) return;
			_init = true;
			_db.Seek((long)_ptr);
			var treePtr = _db.Reader.ReadUInt64();
			if (treePtr != 0) {
				RootNode = new(_db, this, treePtr);
			}
		}

		public int Count {
			get {
				LazyInit();
				return RootNode?.TotalCount ?? 0;
			}
		}

		public bool IsReadOnly => _db.IsReadOnly;
		public object SyncRoot => this;
		public bool IsSynchronized => false;

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
