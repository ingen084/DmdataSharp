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
	public static class SimpleOAuthAuthorizaticator
	{
		/// <summary>
		/// OAuth認証を行う
		/// </summary>
		/// <param name="client"></param>
		/// <param name="scopes"></param>
		/// <param name="clientId"></param>
		/// <param name="title"></param>
		/// <param name="openUrl"></param>
		/// <param name="listenPrefix"></param>
		/// <param name="timeout"></param>
		/// <returns></returns>
		public async static Task<(string refleshToken, string accessToken, DateTime accessTokenExpire)> AuthorizationAsync(
			HttpClient client,
			string clientId,
			string[] scopes,
			string title,
			Action<string> openUrl,
			string listenPrefix,
			TimeSpan timeout)
		{
			var stateString = "";
			var codeVerifierString = "";
			var challengeCodeString = "";
			var authorizationCode = "";

			using (var random = new RNGCryptoServiceProvider())
			using (var s256 = new SHA256Managed())
			{
				var stateCode = new byte[32];
				random.GetBytes(stateCode);
				stateString = string.Join("", stateCode.Select(c => c.ToString("x2")));

				var challengeCode = new byte[64];
				random.GetBytes(challengeCode);
				codeVerifierString = string.Join("", stateCode.Select(c => c.ToString("x2")));

				challengeCodeString = Convert.ToBase64String(s256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifierString))).Replace("=", "").Replace("+", "-").Replace("/", "_");
			}

			using (var listener = new HttpListener())
			{
				listener.Prefixes.Clear();
				listener.Prefixes.Add(listenPrefix);

				// リスナー開始
				listener.Start();

#pragma warning disable CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
				openUrl($"{OAuthCredential.AUTH_ENDPOINT_URL}?" + await new FormUrlEncodedContent(new Dictionary<string, string>()
				{
					{ "client_id", clientId },
					{ "response_type", "code" },
					{ "redirect_uri", listenPrefix },
					{ "response_mode", "query" },
					{ "scope", string.Join(" ", scopes) },
					{ "state", stateString },
					{ "code_challenge", challengeCodeString },
					{ "code_challenge_method", "S256" },
				}).ReadAsStringAsync());
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

				var mre = new ManualResetEvent(false);

				var listenTask = Task.Run(() =>
				{
					try
					{
						while (true)
						{
							var context = listener.GetContext();
							var request = context.Request;
							var response = context.Response;

							response.ContentType = "text/html";

							// methodを確認
							if (request.HttpMethod != "GET")
							{
								response.StatusCode = (int)HttpStatusCode.NotFound;
								WriteResponseHtml(response.OutputStream, title, "認証に失敗しました(invalid method)。認証し直してください。");
								response.Close();
								continue;
							}
							// stateをチェック
							if (request.QueryString.Get("state") != stateString)
							{
								response.StatusCode = (int)HttpStatusCode.BadRequest;
								WriteResponseHtml(response.OutputStream, title, "認証に失敗しました(state mismatch)。認証し直してください。");
								response.Close();
								continue;
							}
							// キャンセル状態のチェック
							if (request.QueryString.Get("error") is string err)
							{
								response.StatusCode = (int)HttpStatusCode.OK;
								WriteResponseHtml(response.OutputStream, title, $"認証はキャンセルされました({err})。このタブは閉じても問題ありません。");
								response.Close();

								mre.Set();
								return;
							}

							if (request.QueryString.Get("code") is not string code)
							{
								response.StatusCode = (int)HttpStatusCode.BadRequest;
								WriteResponseHtml(response.OutputStream, title, "認証に失敗しました(code not set)。認証し直してください。");
								response.Close();
								continue;
							}

							// アクセストークンを取得
							authorizationCode = code;
							WriteResponseHtml(response.OutputStream, title, "認証が完了しました。このタブは閉じても問題ありません。");
							response.Close();

							mre.Set();
							return;
						}
					}
					catch (HttpListenerException) { }
				});

				if (!await Task.Run(() => mre.WaitOne(timeout)))
					throw new DmdataAuthenticationException("コード認証がタイムアウトしました");

				listener.Stop();
			}

			if (string.IsNullOrWhiteSpace(authorizationCode))
				throw new DmdataAuthenticationException("認証はキャンセルされました");

#pragma warning disable CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
			var response = await client.PostAsync(OAuthCredential.TOKEN_ENDPOINT_URL, new FormUrlEncodedContent(new Dictionary<string, string?>()
				{
					{ "client_id", clientId },
					{ "grant_type", "authorization_code" },
					{ "code", authorizationCode },
					{ "code_verifier", codeVerifierString },
				}));
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。

			if (!response.IsSuccessStatusCode)
			{
				var errorResponse = await JsonSerializer.DeserializeAsync<OAuthErrorResponse>(await response.Content.ReadAsStreamAsync());
				throw new DmdataAuthenticationException($"OAuth認証に失敗しました {errorResponse?.Error}({errorResponse?.ErrorDescription})");
			}
			var result = await JsonSerializer.DeserializeAsync<OAuthTokenResponse>(await response.Content.ReadAsStreamAsync());
			if (result == null)
				throw new DmdataAuthenticationException("レスポンスをパースできませんでした");
			if (result.TokenType != "Bearer")
				throw new DmdataAuthenticationException("Bearerトークン以外は処理できません");
			if (result.RefleshToken is not string refleshToken)
				throw new DmdataAuthenticationException("レスポンスからリフレッシュトークンを取得できません");
			if (result.ExpiresIn is not int expiresIn || result.AccessToken is not string accessToken)
				throw new DmdataAuthenticationException("レスポンスからアクセストークンを取得できません");

			return (refleshToken, accessToken, DateTime.Now.AddSeconds(expiresIn));
		}
		private static void WriteResponseHtml(Stream stream, string title, string message)
		{
			var data = CreateResponseHtml(title, message);
			stream.Write(data, 0, data.Length);
		}
		private static byte[] CreateResponseHtml(string title, string message)
			=> Encoding.UTF8.GetBytes($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{title}</title>
</head>
<body>
    <h1>{title}</h1>
    <p>{message}</p>
</body>
</html>");
	}
}
