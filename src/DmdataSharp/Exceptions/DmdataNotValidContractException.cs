namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataで契約が存在しないため情報にアクセスできない例外
	/// </summary>
	public class DmdataNotValidContractException : DmdataException
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataNotValidContractException(string message) : base(message)
		{
		}
	}
}
