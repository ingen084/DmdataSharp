using System;

namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataに関連する例外
	/// </summary>
	public class DmdataException : Exception
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataException(string message) : base(message)
		{
		}
	}
}
