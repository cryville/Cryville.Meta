namespace Cryville.Meta.Util {
	internal static class BitOperations {
		static readonly byte[] Log2DeBruijn = new byte[32] {
			0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
			8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
		};
		public static int Log2(uint value) {
			unchecked {
				value |= value >> 1;
				value |= value >> 2;
				value |= value >> 4;
				value |= value >> 8;
				value |= value >> 16;
				return Log2DeBruijn[value * 130329821 >> 27];
			}
		}
	}
}
