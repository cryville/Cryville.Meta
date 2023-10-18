using System.IO;

namespace Cryville.Meta.Model {
	internal interface IModel {
		void ReadFrom(BinaryReader reader);
		void WriteTo(BinaryWriter writer);
	}
}
