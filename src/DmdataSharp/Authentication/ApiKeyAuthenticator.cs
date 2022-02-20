using DmdataSharp.Exceptions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication
{
	/// <summary>
	/// APIキーによる認証
	/// </summary>
	public class ApiKeyAuthenticator : Authenticator
	{
		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		public ApiKeyAuthenticator(string apiKey)
		{
			ApiKey = apiKey;
		}

		/// <summary>
		/// APIキー
		/// </summary>
		public string ApiKey { get; set; }

		/// <summary>
		/// APIキーを伏せます
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public override string FilterErrorMessage(string message)
			=> message.Replace(ApiKey, "*API_KEY*");

		/// <summary>
		/// リクエストに認証情報を付与し、リクエストを実行します
		/// </summary>
		/// <param name="request">付与するHttpRequestMessage</param>
		/// <param name="sendAsync">リクエストを送信するFunc</param>
		/// <returns>レスポンス</returns>
		public override Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
		{
			if (request.RequestUri is not Uri uri)
				throw new DmdataException("リクエストURIがnullです");

			// keyパラメータを付与する すでにGETパラメータが存在する場合は追加する
#if NET472 || NETSTANDARD2_0
			if (uri.ToString().Contains("?"))
#else
			if (uri.ToString().Contains('?'))
#endif
				request.RequestUri = new Uri(uri + "&key=" + ApiKey);
			else
				request.RequestUri = new Uri(uri + "?key=" + ApiKey);

			return sendAsync(request);
		}
	}
}
