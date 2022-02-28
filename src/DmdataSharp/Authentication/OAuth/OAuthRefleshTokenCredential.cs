﻿using DmdataSharp.ApiResponses.V1;
using DmdataSharp.Exceptions;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		/// DPoPで使用するNonce
		/// </summary>
		private string? DpopNonce { get; set; }

		/// <summary>
		/// 認可コード、リフレッシュトークンによる認証
		/// </summary>
		public OAuthRefreshTokenCredential(HttpClient client, string[] scopes, string clientId, string refreshToken, string? accessToken = null, DateTime? accessTokenExpire = null, ECDsa? dpopKey = null, string? dpopNonce = null) : base(client, scopes)
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
			DpopNonce = dpopNonce;
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
				}!);
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
				if (DpopKey == null && result.TokenType != "Bearer")
					throw new DmdataAuthenticationException("Bearerトークン以外は処理できません");
				if (DpopKey != null && result.TokenType != "DPoP")
					throw new DmdataAuthenticationException("DPoPトークン以外は処理できません");
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
		/// リクエストに認証情報を付与し、リクエストを実行します
		/// </summary>
		/// <param name="request">付与するHttpRequestMessage</param>
		/// <param name="sendAsync">リクエストを送信するFunc</param>
		/// <returns>レスポンス</returns>
		public async override Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
		{
			if (DpopKey == null)
				return await base.ProcessRequestAsync(request, sendAsync);
			SetDpopJwtHeader(request, DpopKey, await GetOrUpdateAccessTokenAsync(), DpopNonce);

			var response = await sendAsync(request);

			// 新しいNonceを取得
			if (response.Headers.TryGetValues("DPoP-Nonce", out var newNonces))
			{
				var newNonce = newNonces.FirstOrDefault();

				// スレッドセーフにするため再送が完了するまで他のリクエストでは新しいNonceを使用させない
				if (!string.IsNullOrWhiteSpace(newNonce) && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					// 再送はできないのでインスタンスを作り直す
					using var newRequest = new HttpRequestMessage(request.Method, request.RequestUri);
					newRequest.Version = request.Version;
					if (request.Content != null)
					{
						var ms = new MemoryStream();
						await request.Content.CopyToAsync(ms);
						ms.Position = 0;
						newRequest.Content = new StreamContent(ms);
						if (request.Content.Headers != null)
							foreach (var h in request.Content.Headers)
								newRequest.Content.Headers.Add(h.Key, h.Value);
					}
#if NETSTANDARD2_0 || NET472
					foreach (var prop in request.Properties)
						newRequest.Properties.Add(prop);
#else
					foreach (var prop in request.Options)
						newRequest.Options.TryAdd(prop.Key, prop.Value);
#endif
					foreach (var header in request.Headers)
						newRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);                   
					
					// 不要になるのでdispose
					response.Dispose();
					request.Dispose();

					// 新しいNonceで再セット
					SetDpopJwtHeader(newRequest, DpopKey, await GetOrUpdateAccessTokenAsync(), newNonce);
					// 再送(1度までしか再送しない)
					response = await sendAsync(newRequest);
				}

				// 再送が終了してから新しいNonceをセット
				DpopNonce = newNonce;
			}

			return response;
		}

		/// <summary>
		/// リフレッシュトークンの詳細を取得する
		/// </summary>
		/// <returns></returns>
		public async override Task<OAuthIntrospectResponse?> IntrospectAsync()
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, INTROSPECT_ENDPOINT_URL);
			request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>()
			{
				{ "client_id", ClientId },
				{ "token", RefreshToken },
			}!);

			using var response = await Client.SendAsync(request);
			return await JsonSerializer.DeserializeAsync<OAuthIntrospectResponse>(await response.Content.ReadAsStreamAsync());
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
			}!);
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
			}!);
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
		/// <param name="accessToken">アクセストークン</param>
		/// <param name="nonce">ナンス</param>
		public static void SetDpopJwtHeader(HttpRequestMessage request, ECDsa key, string? accessToken, string? nonce = null)
		{
#if NET472
			throw new NotSupportedException(".NET Framework はDPoPに対応していません");
#else
			if (accessToken != null)
			{
				request.Headers.Remove("Authorization");
				request.Headers.Add("Authorization", "DPoP " + accessToken);
			}
			request.Headers.Remove("DPoP");
			request.Headers.Add("DPoP", CreateDpopJwt(request, key, accessToken, nonce));
		}

		private static string CreateDpopJwt(HttpRequestMessage request, ECDsa key, string? accessToken, string? nonce)
		{
			if (request.RequestUri is null)
				throw new DmdataAuthenticationException("リクエストURIが存在しません。");

			var builder = JwtBuilder.Create()
				.AddHeader(HeaderName.Type, "dpop+jwt")
				.WithAlgorithm(key.KeySize switch
				{
					256 => new ES256Algorithm(key, key),
					384 => new ES384Algorithm(key, key),
					512 => new ES512Algorithm(key, key),
					_ => throw new DmdataAuthenticationException("この鍵長には対応していません: " + key.KeySize),
				})
				.AddHeader("jwk", GetJwkAnonObject(key))
				.Id(EncodeBase64Url(Guid.NewGuid().ToByteArray()))
				.AddClaim("htm", request.Method.ToString())
				.AddClaim("htu", $"{request.RequestUri.Scheme}://{request.RequestUri.Host}{request.RequestUri.AbsolutePath}")
				.AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

			if (accessToken is not null)
				builder = builder.AddClaim("ath", EncodeBase64Url(SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(accessToken))));
			if (nonce is not null)
				builder = builder.AddClaim("nonce", nonce);

			return builder.Encode();
#endif
		}

		/// <summary>
		/// JWKを表す匿名型を取得します
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public static object GetJwkAnonObject(ECDsa key)
		{
			var param = key.ExportParameters(false);
			if (param.Q.X is null || param.Q.Y is null)
				throw new DmdataAuthenticationException("DPoPに使用する公開鍵のパラメータが取得できません");
			return new {
				crv = "P-" + key.KeySize,
				kty = "EC",
				x = EncodeBase64Url(param.Q.X),
				y = EncodeBase64Url(param.Q.Y),
			};
		}

		/// <summary>
		/// BASE64 URLエンコードを行う
		/// </summary>
		/// <param name="original">もととなるデータ</param>
		/// <returns></returns>
		public static string EncodeBase64Url(byte[] original)
			=> Convert.ToBase64String(original).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}
}
