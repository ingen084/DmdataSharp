using DmdataSharp.Exceptions;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthによるコード認証
	/// </summary>
	public class OAuthRefreshTokenCredential : OAuthCredential
	{
		/// <summary>
		/// クライアントID
		/// </summary>
		public string ClientId { get; }
		/// <summary>
		/// リフレッシュトークン
		/// </summary>
		public string RefreshToken { get; }
		/// <summary>
		/// DPoPに使用する鍵
		/// </summary>
		public ECDsa? DpopKey { get; set; }

		/// <summary>
		/// 認可コード、リフレッシュトークンによる認証
		/// </summary>
		public OAuthRefreshTokenCredential(HttpClient client, string[] scopes, string clientId, string refreshToken, string? accessToken = null, DateTime? accessTokenExpire = null, ECDsa? dpopKey = null) : base(client, scopes)
		{
			ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
			RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
			AccessToken = accessToken;
			AccessTokenExpire = accessTokenExpire;
#if NET472
			if (DpopKey != null)
				throw new NotSupportedException(".NET Framework はDPoPに対応していません");
#endif
			DpopKey = dpopKey;
		}

		/// <summary>
		/// リフレッシュトークンからアクセストークンを取得する
		/// </summary>
		/// <returns></returns>
		protected async override Task<(int, string)> GetAccessTokenAsync()
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_ENDPOINT_URL);
				request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>()
				{
					{ "client_id", ClientId },
					{ "grant_type", "refresh_token" },
					{ "refresh_token", RefreshToken },
				});
				if (DpopKey != null)
					SetDpopJwtHeader(request, DpopKey, null);

				var response = await Client.SendAsync(request);
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
		/// DPoPトークン･JWTを付与する
		/// </summary>
		/// <param name="request">付与するリクエスト</param>
		/// <returns>付与したリクエスト</returns>
		public async override Task<HttpRequestMessage> ProcessRequestMessageAsync(HttpRequestMessage request)
		{
			if (DpopKey == null)
				return await base.ProcessRequestMessageAsync(request);
			SetDpopJwtHeader(request, DpopKey, await GetOrUpdateAccessTokenAsync());
			return request;
		}


		/// <summary>
		/// トークンの無効化
		/// </summary>
		/// <returns></returns>
		public async override Task RevokeAccessTokenAsync()
		{
			if (!TryGetAccessToken(out var token))
				return;
			using var request = new HttpRequestMessage(HttpMethod.Post, REVOKE_ENDPOINT_URL);
			request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>()
			{
				{ "client_id", ClientId },
				{ "token", token },
			});
			if (DpopKey != null)
				SetDpopJwtHeader(request, DpopKey, null);

			using var response = await Client.SendAsync(request);
			if (!response.IsSuccessStatusCode)
				throw new DmdataAuthenticationException("アクセストークンの無効化に失敗しました ");
		}
		/// <summary>
		/// リフレッシュトークンは存在しないため何もしない
		/// </summary>
		/// <returns></returns>
		public async override Task RevokeRefreshTokenAsync()
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, REVOKE_ENDPOINT_URL);
			request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>()
			{
				{ "client_id", ClientId },
				{ "token", RefreshToken },
			});
			if (DpopKey != null)
				SetDpopJwtHeader(request, DpopKey, null);

			using var response = await Client.SendAsync(request);
			if (!response.IsSuccessStatusCode)
				throw new DmdataAuthenticationException("リフレッシュトークンの無効化に失敗しました ");
		}

		/// <summary>
		/// リクエストの内容に応じてDPoPのヘッダをセットする
		/// </summary>
		/// <param name="request">元にするリクエスト</param>
		/// <param name="key">JWTに使用する鍵</param>
		/// <param name="accessToken"></param>
		public static void SetDpopJwtHeader(HttpRequestMessage request, ECDsa key, string? accessToken)
		{
#if NET472
			throw new NotSupportedException(".NET Framework はDPoPに対応していません");
#else
			if (accessToken != null)
				request.Headers.Add("Authorization", "DPoP " + accessToken);
			request.Headers.Add("DPoP", CreateDpopJwt(request, key, accessToken));
		}

		private static string CreateDpopJwt(HttpRequestMessage request, ECDsa key, string? accessToken)
		{
			var param = key.ExportParameters(false);

			if (request.RequestUri is null)
				throw new DmdataAuthenticationException("リクエストURIが存在しません。");
			if (param.Q.X is null || param.Q.Y is null)
				throw new DmdataAuthenticationException("DPoPに使用する公開鍵のパラメータが取得できません");

			var builder = JwtBuilder.Create()
				.AddHeader(HeaderName.Type, "dpop+jwt")
				.WithAlgorithm(new ES512Algorithm(key, key))
				.AddHeader("jwk", new {
					kty = "EC",
					crv = "P-521",
					x = Convert.ToBase64String(param.Q.X).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
					y = Convert.ToBase64String(param.Q.Y).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
				})
				.Id(Guid.NewGuid())
				.AddClaim("htm", request.Method.ToString())
				.AddClaim("htu", $"{request.RequestUri.Scheme}://{request.RequestUri.Host}{request.RequestUri.AbsolutePath}")
				.AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

			if (accessToken is not null)
				builder = builder.AddClaim("ath", Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(accessToken))).TrimEnd('=').Replace('+', '-').Replace('/', '_'));

			return builder.Encode();
#endif
		}
	}
}
