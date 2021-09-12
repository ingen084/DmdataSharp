namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataへのリクエストのレートリミットに引っかかった場合
	/// </summary>
	public class DmdataRateLimitExceededException : DmdataException
	{
		/// <summary>
		/// サーバーから渡された Retry-After の値
		/// </summary>
		public string? RetryAfter { get; }

		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="retryAfter"></param>
		public DmdataRateLimitExceededException(string? retryAfter) : base("レートリミットの上限に達しています")
		{
			RetryAfter = retryAfter;
		}
	}
}
