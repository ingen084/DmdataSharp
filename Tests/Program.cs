using DmdataSharp;
using DmdataSharp.ApiParameters.V2;
using DmdataSharp.Authentication.OAuth;
using DmdataSharp.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
	internal static class Program
	{
		private async static Task Main()
		{
			var builder = DmdataApiClientBuilder.Default
				.UserAgent("DmdataSharp;Example")
				.Referrer(new Uri("http://ingen084.net/"));

			var clientId = "CId.XnvLvldE2-D9lxkLqsXikooQT9pURpYMSXqpQB57s6Rm";
			var scopes = new[] { "contract.list", "telegram.list", "socket.start", "telegram.data", "telegram.get.earthquake", "gd.earthquake" };
			OAuthRefreshTokenCredential credential;
			try
			{
				credential = await SimpleOAuthAuthenticator.AuthorizationAsync(
					builder.HttpClient,
					clientId,
					scopes,
					"DmdataSharp サンプルアプリケーション",
					u =>
					{
						Process.Start(new ProcessStartInfo("cmd", $"/c start {u.Replace("&", "^&")}") { CreateNoWindow = true });
					},
					true);
			}
			catch (Exception ex)
			{
				Console.WriteLine("認証に失敗しました\n" + ex);
				return;
			}

			var introspect = await credential.IntrospectAsync();
			Console.WriteLine("認可したアプリ: " + introspect.Aud);

			builder = builder.UseOAuth(credential);

			using var client = builder.Build<DmdataV2ApiClient>();
			try
			{
				// 電文リストを10件取得してみる
				var telegramList = await client.GetTelegramListAsync(limit: 10, type: "VXSE");
				Console.WriteLine($"** 電文リスト **");
				foreach (var item in telegramList.Items)
				{
					Console.WriteLine($@"** {item.Head.Type} {item.ReceivedTime:yyyy/MM/dd HH:mm:ss} 
  Key: {item.Id}");
				}
				// 1件だけ電文を取得してみる
				Console.WriteLine($"** 電文 **");
				var fi = telegramList.Items.First();
				var cont = await client.GetTelegramStringAsync(fi.Id);
				Console.WriteLine($@"** {fi.Id} length:{cont.Length}");
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
			Console.Write("リフレッシュトークンを無効化");
			await credential.RevokeRefreshTokenAsync();
			Console.WriteLine("しました");
		}
	}
}
