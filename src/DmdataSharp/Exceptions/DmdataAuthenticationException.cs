using System;

namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataの認証に関する例外
	/// </summary>
	public class DmdataAuthenticationException : DmdataException
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataAuthenticationException(string message) : base(message)
		{
		}
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public DmdataAuthenticationException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
