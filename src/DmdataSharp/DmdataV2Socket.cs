using DmdataSharp.ApiParameters.V2;
using DmdataSharp.WebSocketMessages;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DmdataSharp
{
	/// <summary>
	/// dmdataのWebSocket V2セッション
	/// </summary>
	public class DmdataV2Socket : IDisposable
	{
		/// <summary>
		/// WebSocketへの接続が完了した
		/// </summary>
		public event EventHandler<StartWebSocketMessage?>? Connected;
		/// <summary>
		/// errorメッセージが飛んできた
		/// </summary>
		public event EventHandler<ErrorWebSocketMessage?>? Error;
		/// <summary>
		/// WebSocketが切断された
		/// </summary>
		public event EventHandler<EventArgs?>? Disconnected;
		/// <summary>
		/// dataメッセージが飛んできた
		/// </summary>
		public event EventHandler<DataWebSocketMessage?>? DataReceived;

		/// <summary>
		/// WebSocketに接続中かどうか
		/// <para>Connectedイベントが発生する前のコネクション確立時にtrueになる</para>
		/// </summary>
		public bool IsConnected => WebSocket.State == WebSocketState.Open;

		private ClientWebSocket WebSocket { get; } = new ClientWebSocket();
		private CancellationTokenSource? TokenSource { get; set; }
		private Task? WebSocketConnectionTask { get; set; }
		/// <summary>
		/// こちらからPingを送るタイマー
		/// </summary>
		private Timer PingTimer { get; }
		/// <summary>
		/// 受信がなかった場合切断扱いにするタイマー
		/// </summary>
		private Timer WatchDogTimer { get; }
		/// <summary>
		/// 親となるAPIクライアント
		/// </summary>
		public DmdataV2ApiClient ApiClient { get; }
		/// <summary>
		/// WebSocketが切断済みかどうかを取得する
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// WebSocketインスタンスを初期化する
		/// </summary>
		/// <param name="apiClient">親となるAPIクライアント</param>
		public DmdataV2Socket(DmdataV2ApiClient apiClient)
		{
			ApiClient = apiClient;

			WebSocket.Options.AddSubProtocol("dmdata.v2");
			PingTimer = new Timer(_ =>
			{
				if (!IsConnected)
					return;
				WebSocket.SendAsync(
#if NET472 || NETSTANDARD2_0
									new ArraySegment<byte>(
#endif
									JsonSerializer.SerializeToUtf8Bytes(new PingWebSocketMessage() { PingId = DateTime.Now.Ticks.ToString() })
#if NET472 || NETSTANDARD2_0
									)
#endif
									,
					WebSocketMessageType.Text,
					true,
					TokenSource?.Token ?? CancellationToken.None);
			}, null, Timeout.Infinite, Timeout.Infinite);
			WatchDogTimer = new Timer(_ =>
			{
				if (!IsConnected)
					return;
				DisconnectAsync();
			}, null, Timeout.Infinite, Timeout.Infinite);
		}

		/// <summary>
		/// WebSocketに接続する
		/// </summary>
		/// <param name="param">ソケット開始</param>
		/// <returns></returns>
		public async Task ConnectAsync(SocketStartRequestParameter param)
		{
			if (IsConnected)
				throw new InvalidOperationException("すでにWebSocketに接続されています");

			var resp = await ApiClient.GetSocketStartAsync(param);
			TokenSource = new CancellationTokenSource();
			await ConnectAsync(new Uri(resp.Websocket.Url));
		}
		/// <summary>
		/// WebSocketに接続する
		/// </summary>
		/// <param name="uri">接続先のURL</param>
		/// <returns></returns>
		public async Task ConnectAsync(Uri uri)
		{
			if (IsConnected)
				throw new InvalidOperationException("すでにWebSocketに接続されています");

			TokenSource = new CancellationTokenSource();

			await WebSocket.ConnectAsync(uri, TokenSource.Token);
			WebSocketConnectionTask = new Task(async () =>
			{
				try
				{
					// 1MB
					var buffer = new byte[1024 * 1024];

					while (WebSocket.State == WebSocketState.Open)
					{
						// 所得情報確保用の配列を準備
						var segment = new ArraySegment<byte>(buffer);
						// サーバからのレスポンス情報を取得
						var result = await WebSocket.ReceiveAsync(segment, TokenSource.Token);

						// エンドポイントCloseの場合、処理を中断
						if (result.MessageType == WebSocketMessageType.Close)
						{
							Debug.WriteLine("WebSocketが切断されました。");
							await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, TokenSource.Token);
							OnDisconnected();
							return;
						}

						// バイナリは扱わない
						if (result.MessageType == WebSocketMessageType.Binary)
						{
							Debug.WriteLine("WebSocketでBinaryのMessageTypeが飛んできました。");
							await WebSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "DO NOT READ BINARY", TokenSource.Token);
							Disconnected?.Invoke(this, null);
							return;
						}

						// メッセージの最後まで取得
						var length = result.Count;
						while (!result.EndOfMessage)
						{
							if (length >= buffer.Length)
							{
								await WebSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, "TOO LONG MESSAGE", TokenSource.Token);
								Disconnected?.Invoke(this, null);
								return;
							}
							segment = new ArraySegment<byte>(buffer, length, buffer.Length - length);
							result = await WebSocket.ReceiveAsync(segment, TokenSource.Token);

							length += result.Count;
						}

						// 各種タイマーのリセット
						PingTimer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
						WatchDogTimer.Change(TimeSpan.FromMinutes(2), Timeout.InfiniteTimeSpan);

						var messageString = Encoding.UTF8.GetString(buffer, 0, length);
						Debug.WriteLine("resv: " + messageString);
						var message = JsonSerializer.Deserialize<DmdataWebSocketMessage>(messageString);
						switch (message?.Type)
						{
							case "data":
								var dataMessage = JsonSerializer.Deserialize<DataWebSocketMessage>(messageString);
								DataReceived?.Invoke(this, dataMessage);
								break;
							case "start":
								var startMessage = JsonSerializer.Deserialize<StartWebSocketMessage>(messageString);
								Connected?.Invoke(this, startMessage);
								break;
							case "error":
								var errorMessage = JsonSerializer.Deserialize<ErrorWebSocketMessage>(messageString);
								Debug.WriteLine("エラーメッセージを受信しました。");
								Error?.Invoke(this, errorMessage);
								// 切断の場合はそのまま切断する
								if (errorMessage?.Close ?? false)
								{
									Debug.WriteLine("切断要求のため切断扱いとします。");
									await WebSocket.CloseAsync(WebSocketCloseStatus.Empty, null, TokenSource.Token);
									Disconnected?.Invoke(this, null);
									return;
								}
								break;
							// 何もしない
							case "pong":
								break;
							// pongを返す
							case "ping":
								var pingMessage = JsonSerializer.Deserialize<PingWebSocketMessage>(messageString);
								await WebSocket.SendAsync(
#if NET472 || NETSTANDARD2_0
									new ArraySegment<byte>(
#endif
									JsonSerializer.SerializeToUtf8Bytes(new PongWebSocketMessage(pingMessage))
#if NET472 || NETSTANDARD2_0
									)
#endif
									,
									WebSocketMessageType.Text,
									true,
									TokenSource.Token);
								break;
						}
					}
				}
				catch (TaskCanceledException)
				{
					if (IsConnected)
						await WebSocket.CloseAsync(WebSocketCloseStatus.Empty, null, TokenSource.Token);
					OnDisconnected();
				}
				catch (Exception ex)
				{
					Debug.WriteLine("WebSocket受信スレッドで例外が発生しました\n" + ex);
					await WebSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "CLIENT EXCEPTED", TokenSource.Token);
					OnDisconnected();
				}
			}, TokenSource.Token, TaskCreationOptions.LongRunning);
			WebSocketConnectionTask.Start();
		}

		/// <summary>
		/// 切断イベントを呼ぶ
		/// </summary>
		private void OnDisconnected()
		{
			IsDisposed = true;
			PingTimer.Change(Timeout.Infinite, Timeout.Infinite);
			WatchDogTimer.Change(Timeout.Infinite, Timeout.Infinite);
			Disconnected?.Invoke(this, null);
		}

		/// <summary>
		/// WebSocketから切断する
		/// </summary>
		/// <returns></returns>
		public Task DisconnectAsync()
		{
			if (!IsConnected || WebSocketConnectionTask == null)
				return Task.CompletedTask;
			TokenSource?.Cancel();
			return WebSocketConnectionTask;
		}

		/// <summary>
		/// オブジェクトを破棄する
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed)
				WebSocket.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
