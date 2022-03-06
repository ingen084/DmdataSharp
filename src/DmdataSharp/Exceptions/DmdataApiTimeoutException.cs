namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataへのリクエストにタイムアウトした例外
	/// </summary>
	public class DmdataApiTimeoutException : DmdataException
	{
		/// <summary>
		/// リクエストしようとしたURL
		/// </summary>
		public string Url { get; }

		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="url">リクエストしようとしたURL</param>
		public DmdataApiTimeoutException(string url) : base("dmdataへのリクエストにタイムアウトしました。 URL: " + url)
		{
			Url = url;
		}
	}
}
