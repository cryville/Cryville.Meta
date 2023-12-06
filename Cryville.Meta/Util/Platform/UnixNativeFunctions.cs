using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Cryville.Meta.Util.Platform {
	internal class UnixNativeFunctions : NativeWrapper {
		[SuppressMessage("Style", "IDE1006")]
		struct f_statvfs {
			public ulong f_bsize;
			public ulong f_frsize;
			public ulong f_blocks;
			public ulong f_bfree;
			public ulong f_bavail;
			public ulong f_files;
			public ulong f_ffree;
			public ulong f_favail;
			public ulong f_fsid;
			public ulong f_flag;
			public ulong f_namemax;
		}
#if NET45_OR_GREATER || NETSTANDARD1_1_OR_GREATER || NETCOREAPP1_0_OR_GREATER
		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#endif
		[DllImport("c", BestFitMapping = false)]
		static extern int statvfs([MarshalAs(UnmanagedType.LPStr)] string path, out f_statvfs buf);
		public override int GetDiskBlockSize(string path) {
			if (statvfs(path, out var result) != 0)
				throw new InvalidOperationException("statvfs() failed.");
			return (int)result.f_bsize;
		}
	}
}
