namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataで情報にアクセスできない例外
	/// </summary>
	public class DmdataForbiddenException : DmdataException
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataForbiddenException(string message) : base(message)
		{
		}
	}
}
