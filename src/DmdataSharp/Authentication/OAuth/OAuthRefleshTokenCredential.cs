using DmdataSharp.Exceptions;
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
					var errorResponse = await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), OAuthSerializerContext.Default.OAuthErrorResponse);
					throw new DmdataAuthenticationException($"アクセストークンの更新に失敗しました {errorResponse?.Error}({errorResponse?.ErrorDescription})");
				}
				var result = await JsonSerializer.DeserializeAsync(await response.Content.ReadAsStreamAsync(), OAuthSerializerContext.Default.OAuthTokenResponse);
				if (result == null)
					throw new DmdataAuthenticationException("レスポンスをパースできませんでした");
				if (DpopKey == null && result.TokenType != "Bearer")
					throw new DmdataAuthenticationException("Bearerトークン以外は処理できません");
				if (DpopKey != null && result.TokenType != "DPoP")
					throw new DmdataAuthenticationException("DPoPトークン以外は処理できません");
				if (result.ExpiresIn is not int expiresIn || result.AccessToken is not string accessToken)
					throw new DmdataAuthenticationException("レスポンスからトークンを取得できません");
				// スコープが足りてるか確認
				if (Scopes.Except(result.Scope?.Split(' ') ?? Array.Empty<string>()).Any())
					throw new DmdataAuthenticationException("アクセストークンのスコープが足りていません");

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

			var id = EncodeBase64Url(Guid.NewGuid().ToByteArray());

			var sb = new StringBuilder();
			sb.Append(EncodeBase64Url(JsonSerializer.SerializeToUtf8Bytes(
				new JsonWebToken.JwtHeader("dpop+jwt", $"ES{key.KeySize}", new JsonWebKey(key)),
				OAuthSerializerContext.Default.JwtHeader)));
			sb.Append('.');
			sb.Append(EncodeBase64Url(JsonSerializer.SerializeToUtf8Bytes(
				new JsonWebToken.JwtClaim(
					id,
					request.Method.ToString(),
					$"{request.RequestUri.Scheme}://{request.RequestUri.Host}{request.RequestUri.AbsolutePath}",
					DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
					accessToken is null ? null : EncodeBase64Url(SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(accessToken))),
					nonce),
				OAuthSerializerContext.Default.JwtClaim)));
			var alg = key.KeySize switch
			{
				256 => HashAlgorithmName.SHA256,
				384 => HashAlgorithmName.SHA384,
				512 => HashAlgorithmName.SHA512,
				_ => throw new DmdataAuthenticationException("この鍵長には対応していません: " + key.KeySize),
			};
			var signed = EncodeBase64Url(key.SignData(Encoding.ASCII.GetBytes(sb.ToString()), alg));
			sb.Append('.');
			sb.Append(signed);

			return sb.ToString();
#endif
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
