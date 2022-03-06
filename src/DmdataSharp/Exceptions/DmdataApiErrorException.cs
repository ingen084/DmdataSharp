using DmdataSharp.ApiResponses;
using System;

namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataのAPIでエラーが発生した場合
	/// </summary>
	public class DmdataApiErrorException : Exception
	{
		/// <summary>
		/// リクエストID
		/// </summary>
		public string ResponseId { get; set; }

		/// <summary>
		/// レスポンス時刻
		/// </summary>
		public DateTime ResponseTime { get; set; }

		/// <summary>
		/// エラーコード
		/// </summary>
		public int? ErrorCode { get; set; }

		/// <summary>
		/// エラーメッセージ
		/// </summary>
		public string? ErrorMessage { get; set; }

		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="response">元にするレスポンス</param>
		public DmdataApiErrorException(DmdataResponse response)
		{
			ResponseId = response.ResponseId;
			ResponseTime = response.ResponseTime;
			ErrorCode = response.Error?.Code;
			ErrorMessage = response.Error?.Message;
		}
	}
}
