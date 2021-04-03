namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataへのリクエストにタイムアウトした例外
	/// </summary>
	public class DmdataApiTimeoutException : DmdataException
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataApiTimeoutException(string message) : base(message)
		{
		}
	}
}
