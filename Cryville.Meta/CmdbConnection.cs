using Cryville.Meta.Model;
using Cryville.Meta.Util;
using System;
using System.IO;
using System.Threading;

namespace Cryville.Meta {
	/// <summary>
	/// Represents a connection to a CMDB.
	/// </summary>
	public partial class CmdbConnection : IDisposable {
		readonly DirectoryInfo _dir;
		readonly Stream _stream;
		readonly CmdbReader _reader;
		readonly CmdbWriter _writer;
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
				FileMode.OpenOrCreate,
				readOnly ? FileAccess.Read : FileAccess.ReadWrite,
				readOnly ? FileShare.ReadWrite : FileShare.Read,
				FileSystemUtil.GetDiskBlockSize(path),
				FileOptions.RandomAccess
			);
			_reader = new CmdbReader(_stream);
			_writer = new CmdbWriter(_stream);
			_dir = new FileInfo(path).Directory;
			if (_stream.Length == 0) InitDatabase();
			else ReadDatabaseHeader();
		}

		private int m_isDisposed;
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
		int _pageSize;
		void InitDatabase() {
			_pageSize = FileSystemUtil.GetDiskBlockSize(_dir.FullName);
			_sHeader = new CmdbStaticHeader {
				Magic = CmdbMagicNumber,
				Version = 0,
				PageSize = (byte)BitOperations.Log2((uint)_pageSize),
			};
			_dHeader = new CmdbDynamicHeader {
				FileChangeCounter = 0,
				PageCount = 1,
			};
			_copula = new MetonModel {
				RootMetonPairPageIndex = 0,
				SummaryLength = 0,
			};
			_stream.SetLength(2 * _pageSize);
			_writer.Write(_sHeader);
			_stream.Seek(CmdbStaticHeader.Reserved, SeekOrigin.Current);
			_writer.Write(_dHeader);
			_stream.Seek(CmdbDynamicHeader.Reserved, SeekOrigin.Current);
			_writer.Write(_copula);
			_stream.Seek(_pageSize, SeekOrigin.Begin);
			for (int i = 0; i < _pageSize / 8; i++) {
				_writer.Write((ulong)0);
			}
		}
		void ReadDatabaseHeader() {
			_sHeader = _reader.ReadModel<CmdbStaticHeader>();
			_stream.Seek(CmdbStaticHeader.Reserved, SeekOrigin.Current);
			_dHeader = _reader.ReadModel<CmdbDynamicHeader>();
			_stream.Seek(CmdbDynamicHeader.Reserved, SeekOrigin.Current);
			_copula = _reader.ReadModel<MetonModel>();
			if (_sHeader.Magic != CmdbMagicNumber || _sHeader.Version != 0) throw new InvalidDataException("Invalid database.");
			_pageSize = 1 << _sHeader.PageSize;

			_stream.Seek(_pageSize, SeekOrigin.Begin);
			for (int i = 8; i <= _pageSize; i += 8) {
				var ptr = (long)_reader.ReadUInt64();
				if (ptr == 0) continue;
				_rfbcs.Add(new RootFreeBlockCell { Size = i, Pointer = ptr });
			}
		}
	}
}
