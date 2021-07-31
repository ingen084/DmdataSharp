using DmdataSharp.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthによるコード認証
	/// </summary>
	public class OAuthRefreshTokenCredential : OAuthCredential
	{
		private string ClientId { get; set; }
		private string? RefreshToken { get; set; }

		/// <summary>
		/// 認可コード、リフレッシュトークンによる認証
		/// </summary>
		/// <param name="client"></param>
		/// <param name="scopes"></param>
		/// <param name="clientId"></param>
		/// <param name="accessToken"></param>
		/// <param name="accessTokenExpire"></param>
		public OAuthRefreshTokenCredential(HttpClient client, string[] scopes, string clientId, string refreshToken, string? accessToken = null, DateTime? accessTokenExpire = null) : base(client, scopes)
		{
			ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
			RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
			AccessToken = accessToken;
			AccessTokenExpire = accessTokenExpire;
		}

		/// <summary>
		/// リフレッシュトークンからアクセストークンを取得する
		/// </summary>
		/// <returns></returns>
		protected async override Task<(int, string)> GetAccessTokenAsync()
		{
			try
			{
#pragma warning disable CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
				var response = await Client.PostAsync(TOKEN_ENDPOINT_URL, new FormUrlEncodedContent(new Dictionary<string, string?>()
				{
					{ "client_id", ClientId },
					{ "grant_type", "refresh_token" },
					{ "refresh_token", RefreshToken },
				}));
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

				if (!response.IsSuccessStatusCode)
				{
					var errorResponse = await JsonSerializer.DeserializeAsync<OAuthErrorResponse>(await response.Content.ReadAsStreamAsync());
					throw new DmdataAuthenticationException($"アクセストークンの更新に失敗しました {errorResponse?.Error}({errorResponse?.ErrorDescription})");
				}
				var result = await JsonSerializer.DeserializeAsync<OAuthTokenResponse>(await response.Content.ReadAsStreamAsync());
				if (result == null)
					throw new DmdataAuthenticationException("レスポンスをパースできませんでした");
				if (result.TokenType != "Bearer")
					throw new DmdataAuthenticationException("Bearerトークン以外は処理できません");
				if (result.ExpiresIn is not int expiresIn || result.AccessToken is not string accessToken)
					throw new DmdataAuthenticationException("レスポンスからトークンを取得できません");

				return (expiresIn, accessToken);
			}
			catch (Exception ex) when (ex is not DmdataAuthenticationException)
			{
				throw new DmdataAuthenticationException("アクセストークンの更新に失敗しました", ex);
			}
		}


		/// <summary>
		/// トークンの無効化
		/// </summary>
		/// <returns></returns>
		public async override Task RevokeAccessTokenAsync()
		{
			if (!TryGetAccessToken(out var token))
				return;

#pragma warning disable CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
			var response = await Client.PostAsync(REVOKE_ENDPOINT_URL, new FormUrlEncodedContent(new Dictionary<string, string?>()
			{
				{ "client_id", ClientId },
				{ "token", token },
			}));
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

			if (!response.IsSuccessStatusCode)
				throw new DmdataAuthenticationException("アクセストークンの無効化に失敗しました ");
		}
		/// <summary>
		/// リフレッシュトークンは存在しないため何もしない
		/// </summary>
		/// <returns></returns>
		public async override Task RevokeRefreshTokenAsync()
		{
#pragma warning disable CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
			var response = await Client.PostAsync(REVOKE_ENDPOINT_URL, new FormUrlEncodedContent(new Dictionary<string, string?>()
			{
				{ "client_id", ClientId },
				{ "token", RefreshToken },
			}));
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

			if (!response.IsSuccessStatusCode)
				throw new DmdataAuthenticationException("リフレッシュトークンの無効化に失敗しました ");
		}
	}
}
