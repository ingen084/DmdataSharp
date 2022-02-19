﻿using DmdataSharp;
using DmdataSharp.ApiParameters.V2;
using DmdataSharp.Authentication.OAuth;
using DmdataSharp.Exceptions;
using JWT.Builder;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

namespace Tests
{
	internal class Program
	{
		private async static Task Main()
		{
			//Console.WriteLine("DmdataのAPIキーを入力してください>");
			//var apiKey = Console.ReadLine();
			//if (string.IsNullOrWhiteSpace(apiKey))
			//	apiKey = Environment.GetEnvironmentVariable("DMDATA_APIKEY");

			//using var dsa = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP384);
			//var param = dsa.ExportParameters(true);
			//var priv = Convert.ToBase64String(param.D);
			//var jwt = JwtBuilder.Create()
			//	.AddHeader(HeaderName.Type, "dpop+jwt")
			//	.WithAlgorithm(new JWT.Algorithms.ES384Algorithm(dsa, dsa))
			//	.AddHeader("jwk", new {
			//		kty = "EC",
			//		crv = "P-384",
			//		x = Convert.ToBase64String(param.Q.X).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
			//		y = Convert.ToBase64String(param.Q.Y).TrimEnd('=').Replace('+', '-').Replace('/', '_'),
			//	})
			//	.Id(Guid.NewGuid())
			//	.AddClaim("htm", "POST")
			//	.AddClaim("htu", OAuthCredential.TOKEN_ENDPOINT_URL)
			//	.AddClaim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
			//	.Encode();
			//;

			var builder = DmdataApiClientBuilder.Default
				.UserAgent("DmdataSharp;Example")
				.Referrer(new Uri("http://ingen084.net/"));

			var clientId = "CId.XnvLvldE2-D9lxkLqsXikooQT9pURpYMSXqpQB57s6Rm";
			var scopes = new[] { "contract.list", "telegram.list", "socket.start", "telegram.get.earthquake", "gd.earthquake" };
			try
			{
				if (!SimpleOAuthAuthenticator.TryFindUnusedPort(out var port))
					throw new Exception("空きポートが見つかりません");

				var credential = await SimpleOAuthAuthenticator.AuthorizationAsync(
					builder.HttpClient,
					clientId,
					scopes,
					"DmdataSharp サンプルアプリケーション",
					u =>
					{
						Process.Start(new ProcessStartInfo("cmd", $"/c start {u.Replace("&", "^&")}") { CreateNoWindow = true });
					},
					true);
				builder = builder.UseOAuth(credential);
			}
			catch (Exception ex)
			{
				Console.WriteLine("認証に失敗しました\n" + ex);
				return;
			}
			//.UseApiKey(apiKey)
			//.UseOAuthClientCredential(
			//	"CId...",
			//	"CSt...",
			//	new[] { "contract.list", "telegram.list", "socket.start", "telegram.get.earthquake", "telegram.get.volcano", "telegram.get.weather", "telegram.get.scheduled" })


			using var client = builder.Build<DmdataV2ApiClient>();
			try
			{
				// 電文リストを10件取得してみる
				var telegramList = await client.GetTelegramListAsync(limit: 10);
				Console.WriteLine($"** 電文リスト **\n");
				foreach (var item in telegramList.Items)
				{
					Console.WriteLine($@"** {item.Head.Type} {item.ReceivedTime:yyyy/MM/dd HH:mm:ss} 
  Key: {item.Id}");
				}
			}
			catch (DmdataForbiddenException)
			{
				Console.WriteLine("APIキーが正しくないか、電文リストが取得できませんでした。 telegram.list 権限が必要です。");
			}

			try
			{
				// 地震情報を取得
				var events = await client.GetEarthquakeEventsAsync();
				Console.WriteLine("** 地震情報 **");
				foreach (var item in events.Items)
				{
					Console.WriteLine($"{item.OriginTime:yyyy/MM/dd HH:mm:ss} {item.Id}({item.EventId}) {item.Hypocenter?.Name} 最大震度{item.MaxInt}");
					var ev = await client.GetEarthquakeEventAsync(item.EventId);
					foreach (var t in ev.Event.Telegrams)
						Console.WriteLine($"- {t.Id}");
				}
			}
			catch (DmdataForbiddenException)
			{
				Console.WriteLine("APIキーが正しくないか、課金情報の取得ができませんでした。 gd.earthquake 権限が必要です。");
			}
			Console.WriteLine("WebSocketへの接続を行います。 Enterキーで接続");
			Console.ReadLine();

			{
				var socket = new DmdataV2Socket(client);
				socket.Connected += (s, e) => Console.WriteLine("EVENT: connected");
				socket.Disconnected += (s, e) => Console.WriteLine("EVENT: disconnected");
				socket.Error += (s, e) => Console.WriteLine("EVENT: error  c:" + e.Code + " e:" + e.Error);
				socket.DataReceived += (s, e) =>
				{
					Console.WriteLine($@"EVENT: data  type: {e.Head.Type} key: {e.Id} valid: {e.Validate()}
      body: {e.GetBodyString()[..20]}...");
				};
				await socket.ConnectAsync(new SocketStartRequestParameter(
					TelegramCategoryV1.Earthquake
				)
				{
					AppName = "DmdataSharp;Example",
				});

				Console.ReadLine();
				await socket.DisconnectAsync();
			}

			if (client.Authenticator is OAuthAuthenticator authenticator)
				await authenticator.Credential.RevokeRefreshTokenAsync();
		}
	}
}
