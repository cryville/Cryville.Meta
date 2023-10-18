using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Cryville.Meta {
	public partial class CmdbConnection {
		readonly List<RootFreeBlockCell> _rfbcs = new List<RootFreeBlockCell>();
		struct RootFreeBlockCell : IComparable<RootFreeBlockCell> {
			public int Size;
			public long Pointer;
			public bool NewFlag;
			public int CompareTo(RootFreeBlockCell other) => Size.CompareTo(other.Size);
		}
		static int GetAlignedSize(int size) {
			var alignedSize = size & ~0x07;
			if ((size & 0x07) != 0) alignedSize += 0x08;
			return alignedSize;
		}
		void FindFreeBlock(int size, out RootFreeBlockCell targetLargeBlock, out RootFreeBlockCell nextSmallBlock) {
			int alignedSize = GetAlignedSize(size);
			var index = _rfbcs.BinarySearch(new RootFreeBlockCell { Size = alignedSize });
			if (index < 0) {
				index = ~index;
				if (index >= _rfbcs.Count) {
					var len = _stream.Length;
					_stream.SetLength(len + _pageSize);
					targetLargeBlock = new RootFreeBlockCell { Size = _pageSize, Pointer = len, NewFlag = true };
				}
				else {
					targetLargeBlock = _rfbcs[index];
				}
				var index2 = _rfbcs.BinarySearch(new RootFreeBlockCell { Size = targetLargeBlock.Size - alignedSize });
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
			_stream.Seek(_pageSize + block.Size - 0x08, SeekOrigin.Begin);
			_writer.Write(block.Pointer);
		}
		readonly struct BlockScope : IDisposable {
			readonly CmdbConnection _self;
			readonly int _size;
			readonly RootFreeBlockCell _lfb;
			readonly RootFreeBlockCell _sfb;
			readonly ulong _ptrNextLargeBlock;
			internal BlockScope(CmdbConnection self, int size) {
				Debug.Assert(size > 0 && size <= self._pageSize);
				_self = self;
				_size = size;
				_self.FindFreeBlock(size, out _lfb, out _sfb);
				_self._stream.Seek(_lfb.Pointer, SeekOrigin.Begin);
				if (_lfb.NewFlag) {
					_ptrNextLargeBlock = 0;
				}
				else {
					_ptrNextLargeBlock = _self._reader.ReadUInt64();
					_self._stream.Seek(_lfb.Pointer, SeekOrigin.Begin);
				}
			}
			public void Dispose() {
				Debug.Assert(_self._stream.Position - _lfb.Pointer == _size);
				var alignedSize = GetAlignedSize(_size);
				if (alignedSize < _lfb.Size) {
					// A new small block is produced
					var newSmallBlock = new RootFreeBlockCell { Pointer = _lfb.Pointer + alignedSize, Size = _lfb.Size - alignedSize };
					_self._writer.Write((ulong)_sfb.Pointer);
					_self.PushFreeBlock(newSmallBlock);
				}
				_self.PushFreeBlock(new RootFreeBlockCell { Size = _lfb.Size, Pointer = (long)_ptrNextLargeBlock });
			}
		}
		BlockScope AcquireFreeBlock(int size) => new BlockScope(this, size);
		void ReleaseBlock(ulong ptr, int size) {
			Debug.Assert(ptr > 0 && size > 0);
			int alignedSize = GetAlignedSize(size);
			var fb = new RootFreeBlockCell { Size = alignedSize, Pointer = (long)ptr };
			var index = _rfbcs.BinarySearch(fb);
			var nextFreeBlockPtr = index < 0 ? 0 : _rfbcs[index].Pointer;
			_stream.Seek((long)ptr, SeekOrigin.Begin);
			_writer.Write((ulong)nextFreeBlockPtr);
			PushFreeBlock(fb);
		}
	}
}
