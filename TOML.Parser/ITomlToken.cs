namespace TOML
{
	/// <summary>
	/// Mostly a marker token.
	/// </summary>
	public interface ITomlToken
	{
		/// <summary>
		/// In most cases, this is going to be a simple ToString().
		/// </summary>
		/// <returns>String representation of the token.</returns>
		string GetOutput();
	}
}