using DmdataSharp.Exceptions;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
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
	public static class SimpleOAuthAuthenticator
	{
		/// <summary>
		/// エフェメラルポートのうち、未使用のTCPポートを検索する
		/// </summary>
		/// <param name="port">TCPポート</param>
		/// <returns>発見できたか</returns>
		private static bool TryFindUnusedPort(out ushort port)
		{
			// エフェメラルポートの範囲
			ushort min = 49152;
			var max = ushort.MaxValue;

			var props = IPGlobalProperties.GetIPGlobalProperties();
			var conns = props.GetActiveTcpConnections();

			var random = new Random();
			// 100回まではランダムに探す
			foreach (var i in Enumerable.Range(0, 100))
			{
				var p = (ushort)random.Next(min, max);
				if (conns.Any(c => c.LocalEndPoint.Port == p))
					continue;
				port = p;
				return true;
			}

			// 見つからなければ総当り
			for (var p = min; p <= max; p++)
			{
				if (conns.Any(c => c.LocalEndPoint.Port == p))
					continue;
				port = p;
				return true;
			}
			port = 0;
			return false;
		}

		/// <summary>
		/// 認可コードフローによってOAuth認可を得る
		/// <para>まれにListenするポートの問題で <c>HttpListenerException</c> が発生することがあります。適切に対処してください。</para>
		/// </summary>
		/// <param name="client">リクエストに使用するHttpClient</param>
		/// <param name="scopes">認可を求めるスコープ</param>
		/// <param name="clientId">認可を求めるクライアントID</param>
		/// <param name="title">認可後の画面に表示されるクライアント名</param>
		/// <param name="openUrl">URLを開くロジック</param>
		/// <param name="token">CancellationToken 任意のタイミングで処理を中断させたい場合必須</param>
		/// <param name="useDpop">DPoPを使用するか ※まだ試験中の機能のため実験目的以外の利用は推奨しません</param>
		/// <param name="listenPort">内部でホストするHTTPサーバーのポート 未指定の場合はランダム</param>
		/// <returns>認可情報</returns>
		public async static Task<OAuthRefreshTokenCredential> AuthorizationAsync(
			HttpClient client,
			string clientId,
			string[] scopes,
			string title,
			Action<string> openUrl,
			bool useDpop = true,
			CancellationToken? token = null,
			ushort? listenPort = null)
		{
#if NET472
			if (useDpop)
				throw new NotSupportedException(".NET Framework はDPoPに対応していません");
#endif
			var cancellationToken = token ?? CancellationToken.None;
			// ポートが指定されなかった場合ポートを探す
			if (listenPort is not ushort lp && !TryFindUnusedPort(out lp))
				throw new DmdataAuthenticationException("空きポートが見つかりません");

			var listenPrefix = $"http://127.0.0.1:{lp}/";
			var stateString = "";
			var codeVerifierString = "";
			var challengeCodeString = "";
			var authorizationCode = "";
			ECDsa? dsa = null;

			using (var random = RandomNumberGenerator.Create())
			using (var s256 = SHA256.Create())
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
				}!).ReadAsStringAsync());
				var mre = new ManualResetEvent(false);

				cancellationToken.Register(() => mre.Set());
				var listenTask = Task.Run(async () =>
				{
					try
					{
						while (true)
						{
							var context = await listener.GetContextAsync();
							var request = context.Request;
							var response = context.Response;

							response.ContentType = "text/html";

							// pathを確認
							switch (request.Url?.AbsolutePath)
							{
								// 問題ないものは素通し
								case "/":
								case null:
									break;
								// キャンセルはPOSTでのみ処理する
								case "/cancel" when request.HttpMethod == "POST":
									response.StatusCode = (int)HttpStatusCode.OK;
									WriteResponseHtml(response.OutputStream, title, "認証をキャンセルしました。このタブは閉じても問題ありません。", true);
									response.Close();
									return;
								default:
									response.StatusCode = (int)HttpStatusCode.NotFound;
									WriteResponseHtml(response.OutputStream, title, "エンドポイントが見つかりません", false);
									response.Close();
									continue;
							}
							// methodを確認
							if (request.HttpMethod != "GET")
							{
								response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
								WriteResponseHtml(response.OutputStream, title, "認証に失敗しました(invalid method)。認証し直してください。", false);
								response.Close();
								continue;
							}
							// stateをチェック
							if (request.QueryString.Get("state") != stateString)
							{
								response.StatusCode = (int)HttpStatusCode.BadRequest;
								WriteResponseHtml(response.OutputStream, title, "認証に失敗しました(state mismatch)。認証し直してください。", false);
								response.Close();
								continue;
							}
							// キャンセル状態のチェック
							if (request.QueryString.Get("error") is string err)
							{
								response.StatusCode = (int)HttpStatusCode.OK;
								if (err == "cancel")
									WriteResponseHtml(response.OutputStream, title, $"認証をキャンセルしました。このタブは閉じても問題ありません。", true);
								else
									WriteResponseHtml(response.OutputStream, title, $"認可されませんでした({err})。このタブは閉じても問題ありません。", true);
								response.Close();
								return;
							}

							if (request.QueryString.Get("code") is not string code)
							{
								response.StatusCode = (int)HttpStatusCode.BadRequest;
								WriteResponseHtml(response.OutputStream, title, "認証に失敗しました(code not set)。認証し直してください。", false);
								response.Close();
								continue;
							}

							// アクセストークンを取得
							authorizationCode = code;
							WriteResponseHtml(response.OutputStream, title, "認可されました。このタブは閉じても問題ありません。", true);
							response.Close();
							return;
						}
					}
					catch (HttpListenerException) { }
					finally
					{
						mre.Set();
					}
				}, cancellationToken);

				// CancellationTokenが呼ばれるか処理が完了するまで待機する
				await Task.Run(() => mre.WaitOne());
				listener.Stop();
				await listenTask;
			}

			if (string.IsNullOrWhiteSpace(authorizationCode) || cancellationToken.IsCancellationRequested)
				throw new DmdataAuthenticationException("認証はキャンセルされました");

			using var request = new HttpRequestMessage(HttpMethod.Post, OAuthCredential.TOKEN_ENDPOINT_URL);
			request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>()
			{
				{ "client_id", clientId },
				{ "grant_type", "authorization_code" },
				{ "code", authorizationCode },
				{ "redirect_uri", listenPrefix },
				{ "code_verifier", codeVerifierString },
			}!);

#if !NET472
			if (useDpop)
			{
				dsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
				OAuthRefreshTokenCredential.SetDpopJwtHeader(request, dsa, null);
			}
#endif

			using var response = await client.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				var errorResponse = await JsonSerializer.DeserializeAsync<OAuthErrorResponse>(await response.Content.ReadAsStreamAsync());
				throw new DmdataAuthenticationException($"OAuth認証に失敗しました {errorResponse?.Error}({errorResponse?.ErrorDescription})");
			}

			var result = await JsonSerializer.DeserializeAsync<OAuthTokenResponse>(await response.Content.ReadAsStreamAsync());
			if (result == null)
				throw new DmdataAuthenticationException("レスポンスをパースできませんでした");
			if (!useDpop && result.TokenType != "Bearer")
				throw new DmdataAuthenticationException("Bearerトークン以外は処理できません");
			if (useDpop && result.TokenType != "DPoP")
				throw new DmdataAuthenticationException("DPoP使用中はDPoPトークン以外は処理できません");
			if (result.RefreshToken is not string refreshToken)
				throw new DmdataAuthenticationException("レスポンスからリフレッシュトークンを取得できません");
			if (result.ExpiresIn is not int expiresIn || result.AccessToken is not string accessToken)
				throw new DmdataAuthenticationException("レスポンスからアクセストークンを取得できません");

			// DPoP-Nonceが存在する場合はそれを使用する
			if (response.Headers.TryGetValues("DPoP-Nonce", out var nonce))
				return new(client, scopes, clientId, refreshToken, accessToken, DateTime.Now.AddSeconds(expiresIn), dsa, nonce.First());

			return new(client, scopes, clientId, refreshToken, accessToken, DateTime.Now.AddSeconds(expiresIn), dsa);
		}
		private static void WriteResponseHtml(Stream stream, string title, string message, bool isSuccess)
		{
			var data = CreateResponseHtml(title, message, isSuccess);
			stream.Write(data, 0, data.Length);
		}
		private static byte[] CreateResponseHtml(string title, string message, bool isSuccess)
			=> Encoding.UTF8.GetBytes($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{title}</title>
</head>
<body>
    <h1>{title}</h1>
    <p>{message}</p>
	{(isSuccess ? "" : "<form method='post' action='/cancel' id='form'><a href='javascript:form.submit()'>認証をキャンセルする</a></form>")}
</body>
</html>");
	}
}
