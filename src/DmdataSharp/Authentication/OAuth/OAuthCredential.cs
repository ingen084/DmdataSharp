using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthで使用する認可情報
	/// </summary>
	public abstract class OAuthCredential
	{
		/// <summary>
		/// 認可エンドポイント
		/// </summary>
		public const string AUTH_ENDPOINT_URL = "https://manager.dmdata.jp/account/oauth2/v1/auth";
		/// <summary>
		/// トークンエンドポイント
		/// </summary>
		public const string TOKEN_ENDPOINT_URL = "https://manager.dmdata.jp/account/oauth2/v1/token";
		/// <summary>
		/// 失効エンドポイント
		/// </summary>
		public const string REVOKE_ENDPOINT_URL = "https://manager.dmdata.jp/account/oauth2/v1/revoke";
		/// <summary>
		/// 権限チェックエンドポイント
		/// </summary>
		public const string INTROSPECT_ENDPOINT_URL = "https://manager.dmdata.jp/account/oauth2/v1/introspect";

		/// <summary>
		/// 認証情報を初期化する
		/// </summary>
		/// <param name="client">認可に使用するHttpClient</param>
		/// <param name="scopes">認可を求めるスコープ</param>
		protected OAuthCredential(HttpClient client, string[] scopes)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
		}

		/// <summary>
		/// 認可に使用するHttpClient
		/// </summary>
		protected HttpClient Client { get; }

		/// <summary>
		/// 認可を求めるスコープ
		/// </summary>
		protected string[] Scopes { get; }

		/// <summary>
		/// アクセストークン
		/// </summary>
		protected string? AccessToken { get; set; }

		/// <summary>
		/// アクセストークンの有効期限
		/// </summary>
		protected DateTime? AccessTokenExpire { get; set; }

		/// <summary>
		/// 現在保管されているアクセストークンが利用可能かどうか
		/// </summary>
		/// <returns></returns>
		protected bool TryGetAccessToken(out string accessToken)
		{
			if (AccessTokenExpire is not DateTime expireDate || AccessToken is not string token)
			{
				accessToken = null!;
				return false;
			}
			accessToken = token;
			return DateTime.Now < expireDate;
		}

		/// <summary>
		/// Barrerトークンの取得･更新を行う
		/// </summary>
		/// <returns>アクセストークン</returns>
		public async Task<string> GetOrUpdateAccessTokenAsync()
		{
			if (TryGetAccessToken(out var storedToken))
				return storedToken;
			var (expires, token) = await GetAccessTokenAsync();
			AccessTokenExpire = DateTime.Now.AddSeconds(expires);
			return AccessToken = token;
		}

		/// <summary>
		/// リクエストに認証情報を付与し、リクエストを実行します
		/// </summary>
		/// <param name="request">付与するHttpRequestMessage</param>
		/// <param name="sendAsync">リクエストを送信するFunc</param>
		/// <returns>レスポンス</returns>
		public async virtual Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
		{
			request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + await GetOrUpdateAccessTokenAsync());
			return await sendAsync(request);
		}

		// TODO: introspect APIを叩けるようにする


		/// <summary>
		/// アクセストークンを取得する
		/// </summary>
		/// <returns>Bearerトークンと有効期限</returns>
		protected abstract Task<(int, string)> GetAccessTokenAsync();

		/// <summary>
		/// アクセストークンを無効化する
		/// </summary>
		/// <returns></returns>
		public abstract Task RevokeAccessTokenAsync();

		/// <summary>
		/// リフレッシュトークンを無効化する
		/// </summary>
		/// <returns></returns>
		public abstract Task RevokeRefreshTokenAsync();
	}
}
