using System.Net.Http;

namespace DmdataSharp.Authentication
{
	/// <summary>
	/// APIに対する認証を構成します
	/// </summary>
	public abstract class Authenticator
	{
		/// <summary>
		/// リクエストに認証情報を付与します
		/// </summary>
		/// <param name="message">付与するHttpRequestMessage</param>
		/// <returns>付与されたHttpRequestMessage</returns>
		public abstract HttpRequestMessage ProcessRequestMessage(HttpRequestMessage message);

		/// <summary>
		/// エラーメッセージのフィルタリングを行います
		/// </summary>
		/// <param name="message">フィルタリング前のメッセージ</param>
		/// <returns>フィルタリングごのメッセージ</returns>
		public abstract string FilterErrorMessage(string message);
	}
}
