using System;

namespace Cryville.Meta.Util.Platform {
	internal abstract class NativeWrapper {
		static NativeWrapper m_instance;
		public static NativeWrapper Instance {
			get {
				if (m_instance == null) {
					switch (Environment.OSVersion.Platform) {
						case PlatformID.Unix: m_instance = new UnixNativeFunctions(); break;
						case PlatformID.Win32NT: m_instance = new WindowsNativeFunctions(); break;
						default: throw new NotSupportedException();
					}
				}
				return m_instance;
			}
		}

		public abstract int GetDiskBlockSize(string path);
	}
}
