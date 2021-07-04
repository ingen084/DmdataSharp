namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataのアクセス情報が不正な例外
	/// </summary>
	public class DmdataUnauthorizedException : DmdataException
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataUnauthorizedException(string message) : base(message)
		{
		}
	}
}
