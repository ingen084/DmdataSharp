using DmdataSharp;
using DmdataSharp.ApiParameters.V2;
using DmdataSharp.Authentication.OAuth;
using DmdataSharp.Exceptions;
using DmdataSharp.Redundancy;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
			var scopes = new[] { "parameter.earthquake", "contract.list", "telegram.list", "socket.start", "telegram.data", "telegram.get.earthquake", "telegram.get.scheduled", "telegram.get.volcano", "telegram.get.weather", "gd.earthquake", "gd.eew" };
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
						if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
							// Windowsの場合はcmdで開く
							Process.Start(new ProcessStartInfo("cmd", $"/c start {u.Replace("&", "^&")}") { CreateNoWindow = true });
						else
							Console.WriteLine("下記のURLをブラウザを開いて認証を行ってください。\n" + u);
					}
					);// ,true);
			}
			catch (Exception ex)
			{
				Console.WriteLine("認証に失敗しました\n" + ex);
				return;
			}

			builder = builder.UseOAuth(credential);

			using var client = builder.Build<DmdataV2ApiClient>();
			try
			{
				var param = await client.GetEarthquakeStationParameterAsync();
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

			try
			{
				// 最新の緊急地震速報を10件取得
				var events = await client.GetEewEventsAsync(limit: 10);
				Console.WriteLine("** 最新の緊急地震速報 **");
				foreach (var item in events.Items)
				{
					Console.WriteLine($"{item.DateTime:yyyy/MM/dd HH:mm:ss} {item.Id}({item.EventId}) {item.Earthquake.Hypocenter?.Name} 予想最大震度{item.Intensity?.ForecastMaxInt.From}");
					var ev = await client.GetEewEventAsync(item.EventId);
					foreach (var t in ev.Items)
						Console.WriteLine($"- {t.Telegrams.First().Id}");
				}
			}
			catch (DmdataForbiddenException)
			{
				Console.WriteLine("APIキーが正しくないか、課金情報の取得ができませんでした。 gd.eew 権限が必要です。");
			}

			Console.WriteLine("WebSocketへの接続を行います。 Enterキーで接続");
			Console.ReadLine();

			// 通常のWebSocket接続テスト
			Console.WriteLine("=== 通常のWebSocket接続テスト ===");
			using var socket = new DmdataV2Socket(client);
			socket.Connected += (s, e) => Console.WriteLine("EVENT: connected");
			socket.Disconnected += (s, e) => Console.WriteLine("EVENT: disconnected");
			socket.Error += (s, e) => Console.WriteLine("EVENT: error  c:" + e.Code + " e:" + e.Error);
			socket.DataReceived += (s, e) =>
			{
				Console.WriteLine($@"EVENT: data  type: {e.Head.Type} key: {e.Id} valid: {e.Validate()}
      body: {e.GetBodyString()[..20]}...");
			};
			await socket.ConnectAsync(new SocketStartRequestParameter(TelegramCategoryV1.Earthquake)
			{
				AppName = "DmdataSharp;Example",
			}, DmdataV2SocketEndpoints.Osaka);

			Console.WriteLine("通常のWebSocket接続完了。Enterキーで高冗長性WebSocket接続テストに進む");
			Console.ReadLine();
			await socket.DisconnectAsync();

			// 高冗長性WebSocket接続テスト
			Console.WriteLine("=== 高冗長性WebSocket接続テスト ===");
			using var redundantSocket = new RedundantDmdataSocketController(client);
			
			// イベントハンドラ設定
			redundantSocket.ConnectionEstablished += (s, e) => 
				Console.WriteLine($"REDUNDANT: Connection established to {e.EndpointName} at {e.ConnectedTime:HH:mm:ss.fff}");
			
			redundantSocket.ConnectionLost += (s, e) => 
				Console.WriteLine($"REDUNDANT: Connection lost from {e.EndpointName} at {e.DisconnectedTime:HH:mm:ss.fff}. Reason: {e.Reason}");
			
			redundantSocket.AllConnectionsLost += (s, e) => 
				Console.WriteLine($"REDUNDANT: ALL CONNECTIONS LOST at {e.LostTime:HH:mm:ss.fff}. Will attempt reconnect in {e.NextReconnectAttempt.TotalSeconds}s");
			
			redundantSocket.RedundancyRestored += (s, e) => 
				Console.WriteLine($"REDUNDANT: Redundancy restored via {e.RestoredEndpoint} at {e.RestoredTime:HH:mm:ss.fff}. Active connections: {e.TotalActiveConnections}");
			
			redundantSocket.RedundancyStatusChanged += (s, e) => 
				Console.WriteLine($"REDUNDANT: Status changed to {e.Status} at {e.ChangedTime:HH:mm:ss.fff}. Active: {e.ActiveConnections}, Endpoints: [{string.Join(", ", e.ActiveEndpoints)}]");
			
			redundantSocket.ConnectionError += (s, e) => 
				Console.WriteLine($"REDUNDANT: Connection error on {e.EndpointName}: {e.Exception?.Message ?? e.ErrorMessage?.Error}");
			
			redundantSocket.RawDataReceived += (s, e) => 
				Console.WriteLine($"REDUNDANT: Raw data from {e.EndpointName} at {e.ReceivedTime:HH:mm:ss.fff}. Duplicate: {e.IsDuplicate}, Type: {e.Message?.Head.Type}");
			
			redundantSocket.DataReceived += (s, e) =>
			{
				Console.WriteLine($@"REDUNDANT: Final data  type: {e.Head.Type} key: {e.Id} valid: {e.Validate()}
      body: {e.GetBodyString()[..20]}... 
      Stats: Total={redundantSocket.TotalMessagesReceived}, Duplicates={redundantSocket.DuplicateMessagesFiltered}, Active={redundantSocket.ActiveConnectionCount}");
			};

			// デフォルトエンドポイント（東京+大阪）に接続
			await redundantSocket.ConnectAsync(new SocketStartRequestParameter(TelegramCategoryV1.Earthquake)
			{
				AppName = "DmdataSharp",
			});

			Console.WriteLine($"高冗長性WebSocket接続完了。現在の状態: {redundantSocket.Status}, アクティブ接続数: {redundantSocket.ActiveConnectionCount}");
			Console.WriteLine("接続されたエンドポイント: " + string.Join(", ", redundantSocket.ConnectedEndpoints));
			Console.WriteLine("Enterキーで終了");
			Console.ReadLine();
			await redundantSocket.DisconnectAsync();

			Console.Write("リフレッシュトークンを無効化");
			await credential.RevokeRefreshTokenAsync();
			Console.WriteLine("しました");
		}
	}
}
