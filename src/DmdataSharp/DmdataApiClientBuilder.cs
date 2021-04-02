using DmdataSharp.Authentication;
using DmdataSharp.Exceptions;
using System;
using System.Net.Http;

namespace DmdataSharp
{
	/// <summary>
	/// DmdataApiClientの初期化クラス
	/// </summary>
	public class DmdataApiClientBuilder
	{
		private DmdataApiClientBuilder(HttpClient httpClient)
		{
			HttpClient = httpClient;
		}

		/// <summary>
		/// デフォルト構成のBuilderを取得
		/// <para></para>
		/// </summary>
		public static DmdataApiClientBuilder Default
			=> new(new HttpClient() { Timeout = TimeSpan.FromMilliseconds(5000) });

		private HttpClient HttpClient { get; set; }
		private Authenticator? Authenticator { get; set; }

		/// <summary>
		/// 独自のHttpClientを使用する
		/// <para>APIのタイムアウト･UserAgentなどはリセットされます</para>
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public DmdataApiClientBuilder UseOwnHttpClient(HttpClient client)
		{
			HttpClient = client;
			return this;
		}

		/// <summary>
		/// APIのタイムアウトを指定する
		/// </summary>
		/// <param name="time">タイムアウト</param>
		public DmdataApiClientBuilder Timeout(TimeSpan time)
		{
			HttpClient.Timeout = time;
			return this;
		}
		/// <summary>
		/// リファラを設定する
		/// </summary>
		/// <param name="referrer">設定するリファラ</param>
		public DmdataApiClientBuilder Referrer(Uri? referrer)
		{
			HttpClient.DefaultRequestHeaders.Referrer = referrer;
			return this;
		}
		/// <summary>
		/// UserAgentを設定する
		/// </summary>
		/// <param name="userAgent">UserAgent</param>
		public DmdataApiClientBuilder UserAgent(string userAgent)
		{
			HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
			return this;
		}

		/// <summary>
		/// APIキーを使用してAPIの認証を行う
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		public DmdataApiClientBuilder UseApiKey(string apiKey)
		{
			Authenticator = new ApiKeyAuthenticator(apiKey);
			return this;
		}

		/// <summary>
		/// 独自の認証方法でAPIの認証を行う
		/// </summary>
		/// <param name="authenticator">APIキー</param>
		/// <returns></returns>
		public DmdataApiClientBuilder UseAuthenticator(Authenticator authenticator)
		{
			Authenticator = authenticator;
			return this;
		}

		/// <summary>
		/// API V1クライアントの初期化を行う
		/// </summary>
		/// <returns>API V1クライアントのインスタンス</returns>
		[Obsolete]
		public DmdataV1ApiClient BuildV1ApiClient()
		{
			if (Authenticator is null)
				throw new DmdataException("認証方法が指定されていません。 UseApiKey などを使用して認証方法を決定してください。");
			return new DmdataV1ApiClient(HttpClient, Authenticator);
		}
		/// <summary>
		/// API V2クライアントの初期化を行う
		/// </summary>
		/// <returns>API V2クライアントのインスタンス</returns>
		public DmdataV1ApiClient BuildV2ApiClient()
		{
			if (Authenticator is null)
				throw new DmdataException("認証方法が指定されていません。 UseApiKey などを使用して認証方法を決定してください。");
			return new DmdataV1ApiClient(HttpClient, Authenticator);
		}
	}
}
