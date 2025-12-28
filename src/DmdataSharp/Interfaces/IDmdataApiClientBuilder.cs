using DmdataSharp.Authentication;
using DmdataSharp.Authentication.OAuth;
using System;
using System.Net.Http;
using System.Security.Cryptography;

namespace DmdataSharp.Interfaces
{
	/// <summary>
	/// DmdataApiClientBuilderのインターフェイス
	/// </summary>
	public interface IDmdataApiClientBuilder
	{
		/// <summary>
		/// APIコールに使用するHttpClient
		/// </summary>
		HttpClient HttpClient { get; }

		/// <summary>
		/// APIのベースURL
		/// </summary>
		string ApiBaseUrl { get; }

		/// <summary>
		/// データAPIのベースURL
		/// </summary>
		string DataApiBaseUrl { get; }

		/// <summary>
		/// APIのタイムアウトを指定する
		/// </summary>
		/// <param name="time">タイムアウト</param>
		IDmdataApiClientBuilder Timeout(TimeSpan time);

		/// <summary>
		/// リファラを設定する
		/// </summary>
		/// <param name="referrer">設定するリファラ</param>
		IDmdataApiClientBuilder Referrer(Uri? referrer);

		/// <summary>
		/// UserAgentを設定する
		/// </summary>
		/// <param name="userAgent">UserAgent</param>
		IDmdataApiClientBuilder UserAgent(string userAgent);

		/// <summary>
		/// APIキーを使用してAPIの認証を行う
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <returns></returns>
		IDmdataApiClientBuilder UseApiKey(string apiKey);

		/// <summary>
		/// OAuthの認可情報を利用する
		/// </summary>
		/// <param name="credential">認可情報</param>
		/// <returns></returns>
		IDmdataApiClientBuilder UseOAuth(OAuthCredential credential);

		/// <summary>
		/// OAuthのClientCredential認証を行う
		/// </summary>
		/// <param name="clientId">クライアントID</param>
		/// <param name="clientSecret">クライアントシークレット</param>
		/// <param name="scopes">認可を求めるスコープ</param>
		/// <returns></returns>
		[Obsolete("廃止予定です。UseOAuthを利用してください")]
		IDmdataApiClientBuilder UseOAuthClientCredential(string clientId, string clientSecret, string[] scopes);

		/// <summary>
		/// OAuthのリフレッシュトークンによる認証を使用する
		/// </summary>
		/// <param name="clientId">クライアントID</param>
		/// <param name="scopes">認可を求めるスコープ</param>
		/// <param name="refreshToken">リフレッシュトークン</param>
		/// <param name="accessToken">アクセストークン(存在する場合)</param>
		/// <param name="accessTokenExpire">アクセストークンの有効期限</param>
		/// <param name="dpopKey">DPoPに使用する公開鍵と秘密鍵のペア nullの場合DPoPは使用されない</param>
		/// <returns></returns>
		[Obsolete("廃止予定です。UseOAuthを利用してください")]
		IDmdataApiClientBuilder UseOAuthRefreshToken(string clientId, string[] scopes, string refreshToken, string? accessToken, DateTime? accessTokenExpire, ECDsa? dpopKey);

		/// <summary>
		/// 独自の認証方法でAPIの認証を行う
		/// </summary>
		/// <param name="authenticator">APIキー</param>
		/// <returns></returns>
		IDmdataApiClientBuilder UseAuthenticator(Authenticator authenticator);

		/// <summary>
		/// APIのベースURLを設定する
		/// </summary>
		/// <param name="apiBaseUrl">APIのベースURL</param>
		/// <returns></returns>
		IDmdataApiClientBuilder SetApiBaseUrl(string apiBaseUrl);

		/// <summary>
		/// データAPIのベースURLを設定する
		/// </summary>
		/// <param name="dataApiBaseUrl">データAPIのベースURL</param>
		/// <returns></returns>
		IDmdataApiClientBuilder SetDataApiBaseUrl(string dataApiBaseUrl);

		/// <summary>
		/// API V2クライアントの初期化を行う
		/// </summary>
		/// <returns>API V2クライアントのインスタンス</returns>
		IDmdataV2ApiClient BuildV2ApiClient();

		/// <summary>
		/// 任意のAPIクライアントの初期化を行う
		/// </summary>
		/// <returns>API V2クライアントのインスタンス</returns>
		T Build<T>() where T : DmdataApi;
	}
}