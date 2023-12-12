using System.IO;
using UnsafeIL;

namespace Cryville.Meta.Model {
	internal static class ModelExtensions {
		public static T ReadModel<T>(this BinaryReader reader) where T : IModel {
			Unsafe.SkipInit<T>(out var value);
			value.ReadFrom(reader);
			return value;
		}
		public static void Write(this BinaryWriter writer, IModel value) {
			value.WriteTo(writer);
		}
	}
}
