using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication
{
	/// <summary>
	/// APIに対する認証を構成します
	/// </summary>
	public abstract class Authenticator : IDisposable
	{
		/// <summary>
		/// リクエストに認証情報を付与し、リクエストを実行します
		/// </summary>
		/// <param name="request">付与するHttpRequestMessage</param>
		/// <param name="sendAsync">リクエストを送信するFunc</param>
		/// <returns>レスポンス</returns>
		public abstract Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync);

		/// <summary>
		/// エラーメッセージのフィルタリングを行います
		/// </summary>
		/// <param name="message">フィルタリング前のメッセージ</param>
		/// <returns>フィルタリングごのメッセージ</returns>
		public virtual string FilterErrorMessage(string message)
			=> message;

		/// <summary>
		/// オブジェクト･トークンの開放を行います
		/// </summary>
		public virtual void Dispose() => GC.SuppressFinalize(this);
	}
}
