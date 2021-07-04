using DmdataSharp.Authentication;
using DmdataSharp.Authentication.OAuth;
using DmdataSharp.Exceptions;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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
			=> new(new HttpClient(new HttpClientHandler()
			{
#if NET472 || NETSTANDARD2_0
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
#else
				AutomaticDecompression = DecompressionMethods.All
#endif
			})
			{
				Timeout = TimeSpan.FromMilliseconds(5000)
			});

		/// <summary>
		/// APIコールに使用するHttpClient
		/// </summary>
		public HttpClient HttpClient { get; private set; }
		private Authenticator? Authenticator { get; set; }

		/// <summary>
		/// 独自のHttpClientを使用してBuilderを作成する
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static DmdataApiClientBuilder UseOwnHttpClient(HttpClient client)
			=> new(client);

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
		/// OAuthのClientCredential認証を行う
		/// </summary>
		/// <param name="clientId">クライアントID</param>
		/// <param name="clientSecret">クライアントシークレット</param>
		/// <param name="scopes">認可を求めるスコープ</param>
		/// <returns></returns>
		public DmdataApiClientBuilder UseOAuthClientCredential(string clientId, string clientSecret, string[] scopes)
		{
			Authenticator = new OAuthAuthenticator(new OAuthClientCredential(HttpClient, scopes, clientId, clientSecret));
			return this;
		}

		/// <summary>
		/// OAuthのリフレッシュトークンによる認証を使用する
		/// </summary>
		/// <param name="clientId">クライアントID</param>
		/// <param name="scopes">認可を求めるスコープ</param>
		/// <param name="refleshToken">リフレッシュトークン</param>
		/// <param name="accessToken">アクセストークン(存在する場合)</param>
		/// <param name="accessTokenExpire">アクセストークンの有効期限</param>
		/// <returns></returns>
		public DmdataApiClientBuilder UseOAuthRefleshToken(string clientId, string[] scopes, string refleshToken, string? accessToken, DateTime? accessTokenExpire)
		{
			Authenticator = new OAuthAuthenticator(new OAuthRefleshTokenCredential(HttpClient, scopes, clientId, refleshToken, accessToken, accessTokenExpire));
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
		public DmdataV2ApiClient BuildV2ApiClient()
		{
			if (Authenticator is null)
				throw new DmdataException("認証方法が指定されていません。 UseApiKey などを使用して認証方法を決定してください。");
			return new DmdataV2ApiClient(HttpClient, Authenticator);
		}
	}
}
