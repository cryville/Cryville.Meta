using System;

namespace Cryville.Meta.Util.Platform {
	internal abstract class NativeWrapper {
		static NativeWrapper m_instance;
		public static NativeWrapper Instance =>
			m_instance ??= Environment.OSVersion.Platform switch {
				PlatformID.Unix => new UnixNativeFunctions(),
				PlatformID.Win32NT => new WindowsNativeFunctions(),
				_ => throw new NotSupportedException(),
			};

		public abstract int GetDiskBlockSize(string path);
	}
}
