using DmdataSharp.ApiParameters.V2;
using DmdataSharp.WebSocketMessages;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Diagnostics;
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
	public class DmdataV2Socket : Interfaces.IDmdataV2Socket
	{
		private readonly SemaphoreSlim _connectionLock = new(1, 1);

		/// <summary>
		/// 切断時にWebSocketを閉じる処理を待つ最大時間
		/// </summary>
		private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

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
		public bool IsConnected => _webSocket?.State == WebSocketState.Open;

		private ClientWebSocket? _webSocket;
		private CancellationTokenSource? _tokenSource;
		private Task? _webSocketConnectionTask;
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
		public Interfaces.IDmdataV2ApiClient ApiClient { get; }
		/// <summary>
		/// WebSocketが切断済みかどうかを取得する
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// WebSocketインスタンスを初期化する
		/// </summary>
		/// <param name="apiClient">親となるAPIクライアント</param>
		public DmdataV2Socket(Interfaces.IDmdataV2ApiClient apiClient)
		{
			ApiClient = apiClient;

			PingTimer = new Timer(async _ =>
			{
				try
				{
					if (!IsConnected || _webSocket == null)
						return;
					await _webSocket.SendAsync(
#if NET472 || NETSTANDARD2_0
										new ArraySegment<byte>(
#endif
										JsonSerializer.SerializeToUtf8Bytes(new PingWebSocketMessage() { PingId = DateTime.Now.Ticks.ToString() }, WebSocketV2MessageSerializerContext.Default.PingWebSocketMessage)
#if NET472 || NETSTANDARD2_0
										)
#endif
										,
						WebSocketMessageType.Text,
						true,
						_tokenSource?.Token ?? CancellationToken.None);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Socket Send Error {ex}");
					// 送信に失敗した接続は使えないため、受信を止めて切断として扱う
					try
					{
						_tokenSource?.Cancel();
					}
					catch (ObjectDisposedException) { }
				}
			}, null, Timeout.Infinite, Timeout.Infinite);
			WatchDogTimer = new Timer(async _ =>
			{
				try
				{
					if (!IsConnected)
						return;
					await DisconnectAsync();
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"WatchDog Disconnect Error {ex}");
				}
			}, null, Timeout.Infinite, Timeout.Infinite);
		}

		/// <summary>
		/// WebSocketに接続する
		/// </summary>
		/// <param name="param">ソケット開始</param>
		/// <param name="customHostName"><see href="https://dmdata.jp/docs/reference/api/v2/websocket#%E3%82%A8%E3%83%B3%E3%83%89%E3%83%9D%E3%82%A4%E3%83%B3%E3%83%88%E6%83%85%E5%A0%B1">地理冗長化のためなどに接続先のホスト名をカスタムする</see>場合</param>
		/// <returns></returns>
		public async Task ConnectAsync(SocketStartRequestParameter param, string? customHostName = null)
		{
			await _connectionLock.WaitAsync();
			try
			{
				if (IsConnected)
					throw new InvalidOperationException("すでにWebSocketに接続されています");

				var resp = await ApiClient.GetSocketStartAsync(param);
				var uri = new UriBuilder(resp.Websocket.Url);
				if (customHostName != null)
					uri.Host = customHostName;
				await ConnectAsync(uri.Uri);
			}
			finally
			{
				_connectionLock.Release();
			}
		}
		/// <summary>
		/// WebSocketに接続する
		/// </summary>
		/// <param name="uri">接続先のURL</param>
		/// <returns></returns>
		public async Task ConnectAsync(Uri uri)
		{
			// このメソッドは既に_connectionLockの中から呼ばれることを前提とする
			if (IsConnected)
				throw new InvalidOperationException("すでにWebSocketに接続されています");

			// 新しいWebSocketインスタンスを作成
			_webSocket = new ClientWebSocket();
			_webSocket.Options.AddSubProtocol("dmdata.v2");
			_tokenSource = new CancellationTokenSource();

			await _webSocket.ConnectAsync(uri, _tokenSource.Token);
			_webSocketConnectionTask = ReceiverWebSocket();
		}

		private async Task ReceiverWebSocket()
		{
			var token = _tokenSource?.Token ?? throw new Exception("CancellationTokenSource が初期化されていません");
			var webSocket = _webSocket ?? throw new Exception("WebSocket が初期化されていません");
			// 切断時にサーバーへ伝える理由 (null の場合は閉じる処理だけ行う)
			(WebSocketCloseStatus Status, string? Description)? closeReason = null;
			try
			{
				// 1MB
				var buffer = new byte[1024 * 1024];

				while (webSocket.State == WebSocketState.Open)
				{
					// 所得情報確保用の配列を準備
					var segment = new ArraySegment<byte>(buffer);
					// サーバからのレスポンス情報を取得
					var result = await webSocket.ReceiveAsync(segment, token);

					// エンドポイントCloseの場合、処理を中断
					if (result.MessageType == WebSocketMessageType.Close)
					{
						Debug.WriteLine("close message によりWebSocketが切断されました。");
						closeReason = (WebSocketCloseStatus.NormalClosure, null);
						return;
					}

					// バイナリは扱わない
					if (result.MessageType == WebSocketMessageType.Binary)
					{
						Debug.WriteLine("WebSocketでBinaryのMessageTypeが飛んできました。");
						closeReason = (WebSocketCloseStatus.InvalidMessageType, "DO NOT READ BINARY");
						return;
					}

					// メッセージの最後まで取得
					var length = result.Count;
					while (!result.EndOfMessage)
					{
						if (length >= buffer.Length)
						{
							closeReason = (WebSocketCloseStatus.MessageTooBig, "TOO LONG MESSAGE");
							return;
						}
						segment = new ArraySegment<byte>(buffer, length, buffer.Length - length);
						result = await webSocket.ReceiveAsync(segment, token);

						length += result.Count;
					}

					// 各種タイマーのリセット
					PingTimer.Change(TimeSpan.FromMinutes(1), Timeout.InfiniteTimeSpan);
					WatchDogTimer.Change(TimeSpan.FromMinutes(2), Timeout.InfiniteTimeSpan);

					var messageString = Encoding.UTF8.GetString(buffer, 0, length);
					Debug.WriteLine("resv: " + messageString);
					var message = JsonSerializer.Deserialize(messageString, WebSocketV2MessageSerializerContext.Default.DmdataWebSocketMessage);
					switch (message?.Type)
					{
						case "data":
							var dataMessage = JsonSerializer.Deserialize(messageString, WebSocketV2MessageSerializerContext.Default.DataWebSocketMessage);
							DataReceived?.Invoke(this, dataMessage);
							break;
						case "start":
							var startMessage = JsonSerializer.Deserialize(messageString, WebSocketV2MessageSerializerContext.Default.StartWebSocketMessage);
							Connected?.Invoke(this, startMessage);
							break;
						case "error":
							var errorMessage = JsonSerializer.Deserialize(messageString, WebSocketV2MessageSerializerContext.Default.ErrorWebSocketMessage);
							Debug.WriteLine("エラーメッセージを受信しました。");
							Error?.Invoke(this, errorMessage);
							// 切断の場合はそのまま切断する
							if (errorMessage?.Close ?? false)
								_tokenSource.Cancel();
							break;
						// 何もしない
						case "pong":
							break;
						// pongを返す
						case "ping":
							var pingMessage = JsonSerializer.Deserialize(messageString, WebSocketV2MessageSerializerContext.Default.PingWebSocketMessage);
							await webSocket.SendAsync(
#if NET472 || NETSTANDARD2_0
									new ArraySegment<byte>(
#endif
									JsonSerializer.SerializeToUtf8Bytes(new PongWebSocketMessage(pingMessage), WebSocketV2MessageSerializerContext.Default.PongWebSocketMessage)
#if NET472 || NETSTANDARD2_0
									)
#endif
									,
								WebSocketMessageType.Text,
								true,
								token);
							break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				closeReason = (WebSocketCloseStatus.Empty, null);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("WebSocket受信スレッドで例外が発生しました\n" + ex);
				closeReason = (WebSocketCloseStatus.InvalidPayloadData, "CLIENT EXCEPTED");
			}
			finally
			{
				// どの経路で受信を終えても、IsConnected が false になってから切断を通知する
				await CloseWebSocketAsync(webSocket, closeReason);
				OnDisconnected();
			}
		}

		/// <summary>
		/// WebSocketを閉じる<br/>
		/// 閉じられなかった場合は強制的に切断し、例外は投げない
		/// </summary>
		private static async Task CloseWebSocketAsync(ClientWebSocket webSocket, (WebSocketCloseStatus Status, string? Description)? reason)
		{
			try
			{
				if (reason is { } r && webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
				{
					using var timeout = new CancellationTokenSource(CloseTimeout);
					await webSocket.CloseAsync(r.Status, r.Description, timeout.Token);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("WebSocketを正常に閉じられませんでした\n" + ex);
			}
			finally
			{
				// 送信の失敗などでは State が Open のまま残るため、閉じられなかった場合は強制的に切断する
				if (webSocket.State != WebSocketState.Closed)
					webSocket.Abort();
			}
		}

		/// <summary>
		/// 切断イベントを呼ぶ
		/// </summary>
		private void OnDisconnected()
		{
			IsDisposed = true;
			try
			{
				PingTimer.Change(Timeout.Infinite, Timeout.Infinite);
				WatchDogTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
			catch (ObjectDisposedException)
			{
				// Dispose によって受信を終えた場合はタイマーが破棄済み
			}
			Disconnected?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// WebSocketから切断する
		/// </summary>
		/// <returns></returns>
		public async Task DisconnectAsync()
		{
			await _connectionLock.WaitAsync();
			try
			{
				if (!IsConnected || _webSocketConnectionTask == null)
					return;
				
				_tokenSource?.Cancel();
				await _webSocketConnectionTask;
			}
			catch (Exception ex)
			{
				// 受信処理は切断まで行うため、ここで起きた例外は切断の妨げにならない
				Debug.WriteLine("WebSocketの切断待ちで例外が発生しました\n" + ex);
			}
			finally
			{
				_connectionLock.Release();
			}
		}

		/// <summary>
		/// オブジェクトを破棄する
		/// </summary>
		public void Dispose()
		{
			if (!IsDisposed)
			{
				_tokenSource?.Cancel();
				_webSocket?.Dispose();
				_tokenSource?.Dispose();
				PingTimer.Dispose();
				WatchDogTimer.Dispose();
				_connectionLock.Dispose();
			}
			GC.SuppressFinalize(this);
		}
	}
}
