using DmdataSharp.Exceptions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// ClientCredentialsによるOAuth認証
	/// </summary>
	public class OAuthClientCredential : OAuthCredential
	{
		private string ClientId { get; }
		private string ClientSecret { get; }

		/// <summary>
		/// ClientCredentialsによるOAuth認証
		/// </summary>
		/// <param name="client"></param>
		/// <param name="scopes"></param>
		/// <param name="clientId"></param>
		/// <param name="clientSecret"></param>
		public OAuthClientCredential(HttpClient client, string[] scopes, string clientId, string clientSecret) : base(client, scopes)
		{
			ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
			ClientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
		}

		/// <summary>
		/// アクセストークンの取得
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
					{ "client_secret", ClientSecret },
					{ "grant_type", "client_credentials" },
					{ "scope", string.Join(" ", Scopes) },
				}));
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

				if (!response.IsSuccessStatusCode)
				{
					var errorResponse = await JsonSerializer.DeserializeAsync<OAuthErrorResponse>(await response.Content.ReadAsStreamAsync());
					throw new DmdataAuthenticationException($"ClientCredential認証に失敗しました {errorResponse?.Error}({errorResponse?.ErrorDescription})");
				}
				var result = await JsonSerializer.DeserializeAsync<OAuthTokenResponse>(await response.Content.ReadAsStreamAsync());
				if (result == null)
					throw new DmdataAuthenticationException("レスポンスをパースできませんでした");
				if (result.TokenType != "Bearer")
					throw new DmdataAuthenticationException("Bearerトークン以外は処理できません");
				if (result.ExpiresIn is not int expiresIn || result.AccessToken is not string accessToken)
					throw new DmdataAuthenticationException("ClientCredential認証に失敗しました レスポンスからトークンを取得できません");

				return (expiresIn, accessToken);
			}
			catch (Exception ex) when (ex is not DmdataAuthenticationException)
			{
				throw new DmdataAuthenticationException("ClientCredential認証に失敗しました", ex);
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
				{ "client_secret", ClientSecret },
				{ "token", token },
			}));
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

			if (!response.IsSuccessStatusCode)
				throw new DmdataAuthenticationException("ClientCredential認証のアクセストークンの無効化に失敗しました ");
		}
		/// <summary>
		/// リフレッシュトークンは存在しないため何もしない
		/// </summary>
		/// <returns></returns>
		public override Task RevokeRefreshTokenAsync() => Task.CompletedTask;
	}
}
