using Cryville.Meta.Model;
using Cryville.Meta.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Cryville.Meta {
	/// <summary>
	/// Represents a connection to a CMDB.
	/// </summary>
	public partial class CmdbConnection : IDisposable {
		readonly DirectoryInfo _dir;
		readonly Stream _stream;
		internal CmdbReader Reader { get; private set; }
		internal CmdbWriter? Writer { get; private set; }
		/// <summary>
		/// Whether the connection is read-only.
		/// </summary>
		public bool IsReadOnly => !_stream.CanWrite;

		/// <summary>
		/// Creates an instance of the <see cref="CmdbConnection" /> class.
		/// </summary>
		/// <param name="path">The path to the database file.</param>
		public CmdbConnection(string path) : this(path, false) { }
		/// <summary>
		/// Creates an instance of the <see cref="CmdbConnection" /> class.
		/// </summary>
		/// <param name="path">The path to the database file.</param>
		/// <param name="readOnly">Whether to open the database in read-only mode.</param>
		public CmdbConnection(string path, bool readOnly) {
			_stream = new FileStream(
				path,
				readOnly ? FileMode.Open : FileMode.OpenOrCreate,
				readOnly ? FileAccess.Read : FileAccess.ReadWrite,
				readOnly ? FileShare.ReadWrite : FileShare.Read,
				FileSystemUtil.GetDiskBlockSize(path),
				FileOptions.RandomAccess
			);
			Reader = new CmdbReader(_stream);
			if (!readOnly) Writer = new CmdbWriter(_stream);
			_dir = new FileInfo(path).Directory;
			if (_stream.Length == 0) InitDatabase();
			else ReadDatabaseHeader();
			ComputeBTreeParameters();
		}

		int m_isDisposed;
		/// <summary>
		/// Whether the connection has been closed.
		/// </summary>
		public bool IsDisposed => m_isDisposed > 0;
		/// <summary>
		/// Closes the database connection and clean up all resources.
		/// </summary>
		/// <param name="disposing">Whether to clean up managed resources.</param>
		protected virtual void Dispose(bool disposing) {
			if (Interlocked.Exchange(ref m_isDisposed, 1) > 0) return;
			if (disposing) {
				ReleaseMetonPairSets();
				_stream.Dispose();
			}
		}
		/// <summary>
		/// Closes the database connection and clean up all resources.
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		CmdbStaticHeader _sHeader;
		CmdbDynamicHeader _dHeader;
		MetonModel _copula;
		const ulong CmdbMagicNumber = 0x46444d43;
		internal int PageSize { get; private set; }
		void InitDatabase() {
			Debug.Assert(!IsReadOnly);
			try {
				PageSize = FileSystemUtil.GetDiskBlockSize(_dir.FullName);
			}
			catch (InvalidOperationException) {
				PageSize = 512;
			}
			var pageSizeParam = Math.Max((byte)8, Math.Min((byte)21, (byte)BitOperations.Log2((uint)PageSize)));
			PageSize = 1 << pageSizeParam;
			_sHeader = new CmdbStaticHeader {
				Magic = CmdbMagicNumber,
				Version = 0,
				PageSize = pageSizeParam,
			};
			_dHeader = new CmdbDynamicHeader {
				FileChangeCounter = 0,
				PageCount = 1,
			};
			_copula = new MetonModel {
				RootNodePointer = 0,
				SummaryLength = 0,
			};
			_stream.SetLength(2 * PageSize);
			Writer!.Write(_sHeader);
			SeekCurrent(CmdbStaticHeader.Reserved);
			Writer!.Write(_dHeader);
			SeekCurrent(CmdbDynamicHeader.Reserved);
			Writer!.Write(_copula);
			Seek(PageSize);
			for (int i = 0; i < PageSize / 8; i++) {
				Writer!.Write((ulong)0);
			}
		}
		void ReadDatabaseHeader() {
			_sHeader = Reader.ReadModel<CmdbStaticHeader>();
			SeekCurrent(CmdbStaticHeader.Reserved);
			_dHeader = Reader.ReadModel<CmdbDynamicHeader>();
			SeekCurrent(CmdbDynamicHeader.Reserved);
			_copula = Reader.ReadModel<MetonModel>();
			if (_sHeader.Magic != CmdbMagicNumber || _sHeader.Version != 0) throw new InvalidDataException("Invalid database.");
			PageSize = 1 << _sHeader.PageSize;

			Seek(PageSize);
			for (int i = 8; i <= PageSize; i += 8) {
				var ptr = (long)Reader.ReadUInt64();
				if (ptr == 0) continue;
				_rfbcs.Add(new RootFreeBlockCell { Size = i, Pointer = ptr });
			}
		}
		internal void Seek(long offset) => _stream.Seek(offset, SeekOrigin.Begin);
		internal void SeekCurrent(long offset) => _stream.Seek(offset, SeekOrigin.Current);
	}
}
