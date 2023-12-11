using System.Collections.Generic;
using System.Diagnostics;

namespace Cryville.Meta {
	public partial class CmdbConnection {
		readonly Dictionary<ulong, MetonPairSet> _mpss = [];
		internal MetonPairSet OpenMetonPairSet(ulong ptr) {
			Debug.Assert(ptr > 0 && (ptr & 0x07) == 0);
			if (!_mpss.TryGetValue(ptr, out var ret)) {
				_mpss.Add(ptr, ret = new(this, ptr));
			}
			return ret;
		}
		internal void OnMetonPairSetDispose(ulong ptr) {
			_mpss.Remove(ptr);
		}
		void ReleaseMetonPairSets() {
			foreach (var set in _mpss) set.Value.Dispose();
		}

		internal int BTreeOrder;
		internal int BTreeContentAreaOffset;
		internal int BTreeSize;
		void ComputeBTreeParameters() {
			/*
			 * A B-tree page of order N consists of:
			 * - an 8-byte "total count" field,
			 * - N 2-byte cell indices,
			 * - (N+1) 8-byte child pointers,
			 * - N 96-byte meton pairs.
			 * Thus, 106 * N + 16 <= PageSize,
			 * N = (PageSize - 16) / 106.
			 */
			BTreeOrder = (PageSize - 16) / 106;
			BTreeContentAreaOffset = 8 + 10 * BTreeOrder + 8;
			BTreeSize = 106 * BTreeOrder + 16;
		}
	}
}
