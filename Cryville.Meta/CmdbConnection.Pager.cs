using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Cryville.Meta {
	public partial class CmdbConnection {
		readonly List<RootFreeBlockCell> _rfbcs = [];
		struct RootFreeBlockCell : IComparable<RootFreeBlockCell> {
			public int Size;
			public long Pointer;
			public bool NewFlag;
			public RootFreeBlockCell(int size, long pointer = 0, bool newFlag = false) {
				Debug.Assert(size > 0 && (size & 0x07) == 0);
				Size = size;
				Pointer = pointer;
				NewFlag = newFlag;
			}
			public readonly int CompareTo(RootFreeBlockCell other) => Size.CompareTo(other.Size);
		}
		static int GetAlignedSize(int size) {
			var alignedSize = size & ~0x07;
			if ((size & 0x07) != 0) alignedSize += 0x08;
			return alignedSize;
		}
		void FindFreeBlock(int size, out RootFreeBlockCell targetLargeBlock, out RootFreeBlockCell nextSmallBlock) {
			int alignedSize = GetAlignedSize(size);
			var index = _rfbcs.BinarySearch(new(alignedSize));
			if (index < 0) {
				index = ~index;
				if (index >= _rfbcs.Count) {
					var len = _stream.Length;
					_stream.SetLength(len + PageSize);
					targetLargeBlock = new(PageSize, len, true);
				}
				else {
					targetLargeBlock = _rfbcs[index];
				}
				var index2 = _rfbcs.BinarySearch(new(targetLargeBlock.Size - alignedSize));
				nextSmallBlock = index2 < 0 ? default : _rfbcs[index2];
			}
			else {
				targetLargeBlock = _rfbcs[index];
				nextSmallBlock = default;
			}
		}
		void PushFreeBlock(RootFreeBlockCell block) {
			Debug.Assert(block.Size > 0 && (block.Size & 0x07) == 0);
			var index = _rfbcs.BinarySearch(block);
			if (block.Pointer == 0) {
				if (index < 0) return;
				_rfbcs.RemoveAt(index);
			}
			else {
				if (index < 0) index = ~index;
				_rfbcs.Insert(index, block);
			}
			_stream.Position = PageSize + block.Size - 0x08;
			Writer.Write(block.Pointer);
		}
		internal readonly struct BlockScope : IDisposable {
			readonly CmdbConnection _self;
			readonly int _size;
			readonly RootFreeBlockCell _lfb;
			readonly RootFreeBlockCell _sfb;
			readonly ulong _ptrNextLargeBlock;
			internal BlockScope(CmdbConnection self, int size) {
				Debug.Assert(size > 0 && size <= self.PageSize);
				_self = self;
				_size = size;
				_self.FindFreeBlock(size, out _lfb, out _sfb);
				_self.Seek(_lfb.Pointer);
				if (_lfb.NewFlag) {
					_ptrNextLargeBlock = 0;
				}
				else {
					_ptrNextLargeBlock = _self.Reader.ReadUInt64();
					_self.Seek(_lfb.Pointer);
				}
			}
			public void Dispose() {
				Debug.Assert(_self._stream.Position - _lfb.Pointer == _size);
				var alignedSize = GetAlignedSize(_size);
				if (alignedSize < _lfb.Size) {
					// A new small block is produced
					// Seek to the aligned position
					_self.SeekCurrent(alignedSize - _size);
					var newSmallBlock = new RootFreeBlockCell(_lfb.Size - alignedSize, _lfb.Pointer + alignedSize);
					_self.Writer.Write((ulong)_sfb.Pointer);
					_self.PushFreeBlock(newSmallBlock);
				}
				_self.PushFreeBlock(new(_lfb.Size, (long)_ptrNextLargeBlock));
			}
		}
		internal BlockScope AcquireFreeBlock(int size) => new(this, size);
		internal void ReleaseBlock(ulong ptr, int size) {
			Debug.Assert(ptr > 0 && size > 0);
			int alignedSize = GetAlignedSize(size);
			var fb = new RootFreeBlockCell(alignedSize, (long)ptr);
			var index = _rfbcs.BinarySearch(fb);
			var nextFreeBlockPtr = index < 0 ? 0 : _rfbcs[index].Pointer;
			Seek((long)ptr);
			Writer.Write((ulong)nextFreeBlockPtr);
			PushFreeBlock(fb);
		}
	}
}
