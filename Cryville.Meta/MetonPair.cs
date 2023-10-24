namespace Cryville.Meta {
	public struct MetonPair {
		public Meton Key { get; set; }
		public Meton Value { get; set; }
		public MetonPair(Meton key, Meton value) {
			Key = key;
			Value = value;
		}
	}
}
