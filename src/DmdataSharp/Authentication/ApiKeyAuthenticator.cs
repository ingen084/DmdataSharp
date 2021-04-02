using System;
using System.Net.Http;

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
		/// URLにAPIキーを付与します
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public override HttpRequestMessage ProcessRequestMessage(HttpRequestMessage message)
		{
			if (message.RequestUri is not Uri uri)
				throw new Exception("リクエストURIがnullです");

			// keyパラメータを付与する すでにGETパラメータが存在する場合は追加する
			if (uri.ToString().Contains("?"))
				message.RequestUri = new Uri(uri + "&key=" + ApiKey);
			else
				message.RequestUri = new Uri(uri + "?key=" + ApiKey);

			return message;
		}
	}
}
