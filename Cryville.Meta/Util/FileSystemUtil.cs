using Cryville.Meta.Util.Platform;

namespace Cryville.Meta.Util {
	internal static class FileSystemUtil {
		public static int GetDiskBlockSize(string path) => NativeWrapper.Instance.GetDiskBlockSize(path);
	}
}
