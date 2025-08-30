using DmdataSharp.Authentication;
using System;

namespace DmdataSharp.Interfaces
{
	/// <summary>
	/// dmdataのAPIクライアントベースクラスのインターフェイス
	/// </summary>
	public interface IDmdataApi : IDisposable
	{
		/// <summary>
		/// リクエストのタイムアウト時間
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// dmdataのAPI認証方法
		/// </summary>
		Authenticator Authenticator { get; set; }

		/// <summary>
		/// APIのベースURL
		/// </summary>
		string ApiBaseUrl { get; set; }

		/// <summary>
		/// データAPIのベースURL
		/// </summary>
		string DataApiBaseUrl { get; set; }

		/// <summary>
		/// 並列リクエストを許可するか
		/// </summary>
		bool AllowPararellRequest { get; set; }
	}
}