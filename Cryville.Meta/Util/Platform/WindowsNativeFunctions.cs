using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Cryville.Meta.Util.Platform {
	internal class WindowsNativeFunctions : NativeWrapper {
#if NET45_OR_GREATER || NETSTANDARD1_1_OR_GREATER || NETCOREAPP1_0_OR_GREATER
		[DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#endif
		[DllImport("Kernel32.dll")]
		static extern bool GetDiskFreeSpaceW(
			[MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
			out int lpSectorsPerCluster,
			out int lpBytesPerSector,
			out int lpNumberOfFreeClusters,
			out int lpTotalNumberOfClusters
		);
		public override int GetDiskBlockSize(string path) {
			var dir = new DirectoryInfo(path);
			if (!GetDiskFreeSpaceW(dir.Root.FullName, out var spc, out var bps, out _, out _))
				throw new InvalidOperationException("GetDiskFreeSpaceW() failed.");
			return bps * spc;
		}
	}
}
