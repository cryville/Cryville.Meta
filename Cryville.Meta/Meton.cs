using Cryville.Meta.Model;
using System;

namespace Cryville.Meta {
	public class Meton : IEquatable<Meton> {
		readonly CmdbConnection _db;
		public MetonIdentifier Id { get; private set; }
		internal ulong Pointer;

		internal Meton(CmdbConnection db, MetonIdentifier id, ulong pointer) {
			_db = db;
			Id = id;
			Pointer = pointer;
		}

		public bool Equals(Meton other) => Id.Equals(other.Id);
	}
}
