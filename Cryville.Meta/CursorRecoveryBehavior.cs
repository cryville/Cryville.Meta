namespace Cryville.Meta {
	/// <summary>
	/// Specifies how a cursor should be recovered after the collection is modified.
	/// </summary>
	public enum CursorRecoveryBehavior {
		/// <summary>
		/// Throws an exception without recovery.
		/// </summary>
		/// <remarks>
		/// <para>This behavior is fast and safe, but it prevents the cursor from being advanced until it is reset.</para>
		/// </remarks>
		None,
		/// <summary>
		/// Skips the modified portion in the collection.
		/// </summary>
		/// <remarks>
		/// <para>This behavior is fast but unsafe. It may skip some elements.</para>
		/// </remarks>
		Skip,
		/// <summary>
		/// Re-locates the current element in the collection.
		/// </summary>
		/// <remarks>
		/// <para>This behavior is slow but safe. It does not skip any elements.</para>
		/// </remarks>
		Reset,
	}
}
