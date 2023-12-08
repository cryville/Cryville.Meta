using Cryville.Meta.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Cryville.Meta {
	internal class BTreeCursor(MetonPairSet set) : IEnumerator<MetonPairModel>, IEnumerator {
		[SuppressMessage("Usage", "CA2213")]
		readonly MetonPairSet _set = set;
		readonly Stack<Frame> _stack = new();
		struct Frame(BTreeNode node, int index) {
			public BTreeNode Node = node;
			public int Version = node.Version;
			public int Index = index;
		}

		public void Dispose() { }

		public void Reset() {
			_stack.Clear();
			_state = -2;
		}
		public bool Search(MetonPairModel value) {
			Reset();
			_state = 0;
			var node = _set.RootNode;
			if (node == null)
				return false;
			while (true) {
				var index = node.BinarySearch(value, out _);
				if (index < 0) {
					index = ~index;
					_stack.Push(new(node, index));
					if (node.IsLeaf) return false;
					node = node.GetChildNode(index);
				}
				else {
					_stack.Push(new(node, index));
					return true;
				}
			}
		}
		public bool Add(MetonPairModel value) {
			if (Search(value)) return false;
			// TODO
			return true;
		}
		public bool Remove(MetonPairModel value) {
			if (!Search(value)) return false;
			// TODO
			return true;
		}

		int _state = -2;
		MetonPairModel m_current;
		public MetonPairModel Current => m_current;
		object IEnumerator.Current => Current;
		public bool MoveNext() => MoveNext(CursorRecoveryBehavior.Skip);
		public bool MoveNext(CursorRecoveryBehavior recoveryBehavior) {
			if (_state == -2) {
				var root = _set.RootNode;
				if (root != null)
					PushPreorder(new(root, 0));
				_state = 0;
			}
			while (_stack.Count > 0) {
				var frame = _stack.Pop();
				if (frame.Node.IsDisposed) throw new ObjectDisposedException(null);
				if (frame.Version != frame.Node.Version) {
					switch (recoveryBehavior) {
						case CursorRecoveryBehavior.Skip: continue;
						case CursorRecoveryBehavior.Reset:
							var flag = Search(m_current);
							frame = _stack.Pop();
							if (flag) frame.Index++;
							break;
						default: throw new InvalidOperationException("Collection was modified.");
					}
				}
				if (frame.Index >= frame.Node.Count) continue;
				m_current = frame.Node.GetMetonPair(frame.Index++);
				PushPreorder(frame);
				return true;
			}
			_state = -1;
			return false;
		}
		void PushPreorder(Frame frame) {
			while (true) {
				_stack.Push(frame);
				if (frame.Node.IsLeaf) return;
				frame = new(frame.Node.GetChildNode(frame.Index), 0);
			}
		}
	}
}
