using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthで使用する認証情報
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
#pragma warning disable CS8625 // null リテラルを null 非許容参照型に変換できません。
				accessToken = null;
				return false;
#pragma warning restore CS8625 // null リテラルを null 非許容参照型に変換できません。
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
