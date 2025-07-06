using DmdataSharp.ApiParameters.V2;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DmdataSharp.Redundancy;

/// <summary>
/// 高冗長性dmdataのWebSocket V2セッション
/// </summary>
public class DmdataV2RedundantSocket : IDisposable
{
	#region Events

	/// <summary>
	/// 重複排除前の生データが受信された
	/// </summary>
	public event EventHandler<RawDataReceivedEventArgs>? RawDataReceived;

	/// <summary>
	/// 重複排除後のデータが受信された
	/// </summary>
	public event EventHandler<DataWebSocketMessage>? DataReceived;

	/// <summary>
	/// 個別接続が確立された
	/// </summary>
	public event EventHandler<ConnectionEstablishedEventArgs>? ConnectionEstablished;

	/// <summary>
	/// 個別接続が失われた
	/// </summary>
	public event EventHandler<ConnectionLostEventArgs>? ConnectionLost;

	/// <summary>
	/// すべての接続が失われた
	/// </summary>
	public event EventHandler<AllConnectionsLostEventArgs>? AllConnectionsLost;

	/// <summary>
	/// 冗長性が復旧した（切断状態から少なくとも1つの接続が復旧）
	/// </summary>
	public event EventHandler<RedundancyRestoredEventArgs>? RedundancyRestored;

	/// <summary>
	/// 接続でエラーが発生した
	/// </summary>
	public event EventHandler<ConnectionErrorEventArgs>? ConnectionError;

	/// <summary>
	/// 冗長性状態が変更された
	/// </summary>
	public event EventHandler<RedundancyStatusChangedEventArgs>? RedundancyStatusChanged;

	#endregion

	#region Private Fields

	private readonly Dictionary<string, DmdataV2Socket> _connections = [];
	private readonly Dictionary<string, ReconnectionState> _reconnectionStates = [];
	private readonly HashSet<string> _recentMessageIds = [];
	private readonly Queue<string> _messageIdQueue = new();
	private readonly object _lockObject = new();

	private SocketStartRequestParameter? _connectionParameters;
	private RedundancyStatus _currentStatus = RedundancyStatus.Disconnected;
	private bool _disposed = false;

	#endregion

	#region Properties

	/// <summary>
	/// 親となるAPIクライアント
	/// </summary>
	public DmdataV2ApiClient ApiClient { get; }

	/// <summary>
	/// 少なくとも1つの接続が有効かどうか
	/// </summary>
	public bool IsConnected => _connections.Values.Any(s => s.IsConnected);

	/// <summary>
	/// 現在のアクティブ接続数
	/// </summary>
	public int ActiveConnectionCount => _connections.Values.Count(s => s.IsConnected);

	/// <summary>
	/// 接続されているエンドポイント名の配列
	/// </summary>
	public string[] ConnectedEndpoints => _connections.Where(kv => kv.Value.IsConnected).Select(kv => kv.Key).ToArray();

	/// <summary>
	/// 現在の冗長性状態
	/// </summary>
	public RedundancyStatus Status => _currentStatus;

	/// <summary>
	/// 受信した総メッセージ数
	/// </summary>
	public long TotalMessagesReceived { get; private set; }

	/// <summary>
	/// フィルタされた重複メッセージ数
	/// </summary>
	public long DuplicateMessagesFiltered { get; private set; }

	/// <summary>
	/// 最後にメッセージを受信した時刻
	/// </summary>
	public DateTime? LastMessageTime { get; private set; }

	/// <summary>
	/// 設定オプション
	/// </summary>
	public RedundantSocketOptions Options { get; }

	/// <summary>
	/// オブジェクトが破棄済みかどうか
	/// </summary>
	public bool IsDisposed => _disposed;

	#endregion

	#region Constructor

	/// <summary>
	/// 冗長性WebSocketインスタンスを初期化する
	/// </summary>
	/// <param name="apiClient">親となるAPIクライアント</param>
	/// <param name="options">設定オプション</param>
	public DmdataV2RedundantSocket(DmdataV2ApiClient apiClient, RedundantSocketOptions? options = null)
	{
		ApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
		Options = options ?? new RedundantSocketOptions();

	}

	#endregion

	#region Public Methods

	/// <summary>
	/// 複数のエンドポイントに同時接続する
	/// </summary>
	/// <param name="param">ソケット開始パラメータ</param>
	/// <param name="endpoints">接続先エンドポイント（nullの場合はデフォルト）</param>
	/// <returns></returns>
	public async Task ConnectAsync(SocketStartRequestParameter param, string[]? endpoints = null)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(DmdataV2RedundantSocket));

		var targetEndpoints = endpoints ?? RedundantSocketOptions.DefaultEndpoints;
		var wasDisconnected = !IsConnected;

		// 接続パラメータを保存
		_connectionParameters = param;

		foreach (var endpoint in targetEndpoints)
		{
			try
			{
				var socket = new DmdataV2Socket(ApiClient);
				SetupSocketEvents(socket, endpoint);
				_connections[endpoint] = socket;
				_reconnectionStates[endpoint] = new ReconnectionState();

				await socket.ConnectAsync(param, endpoint);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to connect to {endpoint}: {ex.Message}");
				ConnectionError?.Invoke(this, new ConnectionErrorEventArgs
				{
					EndpointName = endpoint,
					Exception = ex
				});

				// 接続失敗時は再接続をスケジュール
				ScheduleReconnect(endpoint);
			}
		}

		UpdateRedundancyStatus();

		// 全接続失敗の場合
		if (!IsConnected)
		{
			AllConnectionsLost?.Invoke(this, new AllConnectionsLostEventArgs
			{
				LostTime = DateTime.Now,
				DisconnectedEndpoints = targetEndpoints,
				WillAttemptReconnect = true,
				NextReconnectAttempt = Options.InitialReconnectDelay
			});

			// 全エンドポイントの再接続をスケジュール
			foreach (var endpoint in targetEndpoints)
			{
				ScheduleReconnect(endpoint);
			}
		}
		else if (wasDisconnected)
		{
			// 切断状態から復旧した場合
			var firstConnectedEndpoint = ConnectedEndpoints.FirstOrDefault() ?? string.Empty;
			RedundancyRestored?.Invoke(this, new RedundancyRestoredEventArgs
			{
				RestoredTime = DateTime.Now,
				RestoredEndpoint = firstConnectedEndpoint,
				TotalActiveConnections = ActiveConnectionCount
			});
		}
	}

	/// <summary>
	/// すべての接続を切断する
	/// </summary>
	/// <returns></returns>
	public async Task DisconnectAsync()
	{
		if (_disposed)
			return;

		var disconnectTasks = _connections.Values.Select(socket => socket.DisconnectAsync()).ToArray();
		await Task.WhenAll(disconnectTasks);

		foreach (var socket in _connections.Values)
		{
			socket.Dispose();
		}

		_connections.Clear();
		UpdateRedundancyStatus();
	}

	/// <summary>
	/// 特定のエンドポイントの再接続を試行する
	/// </summary>
	/// <param name="endpoint">再接続するエンドポイント名</param>
	/// <param name="param">接続パラメータ</param>
	/// <returns></returns>
	public async Task ReconnectEndpointAsync(string endpoint, SocketStartRequestParameter param)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(DmdataV2RedundantSocket));

		if (_connections.TryGetValue(endpoint, out var oldSocket))
		{
			await oldSocket.DisconnectAsync();
			oldSocket.Dispose();
		}

		try
		{
			var socket = new DmdataV2Socket(ApiClient);
			SetupSocketEvents(socket, endpoint);
			_connections[endpoint] = socket;

			await socket.ConnectAsync(param, endpoint);
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to reconnect to {endpoint}: {ex.Message}");
			ConnectionError?.Invoke(this, new ConnectionErrorEventArgs
			{
				EndpointName = endpoint,
				Exception = ex
			});
		}

		UpdateRedundancyStatus();
	}

	#endregion

	#region Private Methods

	private void SetupSocketEvents(DmdataV2Socket socket, string endpoint)
	{
		socket.Connected += (sender, e) => OnSocketConnected(sender, e, endpoint);
		socket.DataReceived += (sender, e) => OnSocketDataReceived(sender, e, endpoint);
		socket.Disconnected += (sender, e) => OnSocketDisconnected(sender, e, endpoint);
		socket.Error += (sender, e) => OnSocketError(sender, e, endpoint);
	}

	private void OnSocketConnected(object? _, StartWebSocketMessage? e, string endpoint)
	{
		var wasDisconnected = Status == RedundancyStatus.Disconnected;

		ConnectionEstablished?.Invoke(this, new ConnectionEstablishedEventArgs
		{
			EndpointName = endpoint,
			StartMessage = e,
			ConnectedTime = DateTime.Now
		});

		UpdateRedundancyStatus();

		if (wasDisconnected)
		{
			RedundancyRestored?.Invoke(this, new RedundancyRestoredEventArgs
			{
				RestoredTime = DateTime.Now,
				RestoredEndpoint = endpoint,
				TotalActiveConnections = ActiveConnectionCount
			});
		}
	}

	private void OnSocketDataReceived(object? _, DataWebSocketMessage? e, string endpoint)
	{
		if (e == null)
			return;

		TotalMessagesReceived++;
		LastMessageTime = DateTime.Now;

		var isDuplicate = IsDuplicateMessage(e.Id);

		RawDataReceived?.Invoke(this, new RawDataReceivedEventArgs
		{
			EndpointName = endpoint,
			Message = e,
			IsDuplicate = isDuplicate,
			ReceivedTime = DateTime.Now
		});

		if (!isDuplicate)
		{
			DataReceived?.Invoke(this, e);
		}
		else
		{
			DuplicateMessagesFiltered++;
		}
	}

	private void OnSocketDisconnected(object? _, EventArgs? __, string endpoint)
	{
		ConnectionLost?.Invoke(this, new ConnectionLostEventArgs
		{
			EndpointName = endpoint,
			DisconnectedTime = DateTime.Now,
			WillReconnect = true,
			Reason = "Connection lost"
		});

		var wasConnected = Status != RedundancyStatus.Disconnected;
		UpdateRedundancyStatus();

		// 個別エンドポイントの再接続をスケジュール
		ScheduleReconnect(endpoint);

		// 全接続が失われた場合
		if (wasConnected && Status == RedundancyStatus.Disconnected)
		{
			AllConnectionsLost?.Invoke(this, new AllConnectionsLostEventArgs
			{
				LostTime = DateTime.Now,
				DisconnectedEndpoints = [.. _connections.Keys],
				WillAttemptReconnect = true,
				NextReconnectAttempt = GetNextReconnectDelay(endpoint)
			});
		}
	}

	private void OnSocketError(object? _, ErrorWebSocketMessage? e, string endpoint) =>
		ConnectionError?.Invoke(this, new ConnectionErrorEventArgs
		{
			EndpointName = endpoint,
			ErrorMessage = e
		});

	private bool IsDuplicateMessage(string messageId)
	{
		lock (_lockObject)
		{
			if (_recentMessageIds.Contains(messageId))
				return true;

			// キャッシュサイズ制限
			if (_messageIdQueue.Count >= Options.DeduplicationCacheSize)
			{
				var oldId = _messageIdQueue.Dequeue();
				_recentMessageIds.Remove(oldId);
			}

			_recentMessageIds.Add(messageId);
			_messageIdQueue.Enqueue(messageId);
			return false;
		}
	}

	private void UpdateRedundancyStatus()
	{
		var activeCount = ActiveConnectionCount;
		var totalCount = _connections.Count;
		var newStatus = RedundancyStatus.Disconnected;

		if (activeCount == 0)
		{
			newStatus = RedundancyStatus.Disconnected;
		}
		else if (activeCount == totalCount)
		{
			newStatus = RedundancyStatus.FullyConnected;
		}
		else if (activeCount >= 1)
		{
			newStatus = activeCount >= totalCount / 2 ? RedundancyStatus.PartiallyConnected : RedundancyStatus.Degraded;
		}

		if (newStatus != _currentStatus)
		{
			_currentStatus = newStatus;
			RedundancyStatusChanged?.Invoke(this, new RedundancyStatusChangedEventArgs
			{
				Status = newStatus,
				ActiveConnections = activeCount,
				ActiveEndpoints = ConnectedEndpoints,
				ChangedTime = DateTime.Now
			});
		}
	}

	private void ScheduleReconnect(string endpoint)
	{
		if (_disposed || _connectionParameters == null)
			return;

		if (!_reconnectionStates.TryGetValue(endpoint, out var state))
		{
			state = new ReconnectionState();
			_reconnectionStates[endpoint] = state;
		}

		if (state.IsReconnecting)
			return; // 既に再接続中

		var delay = state.GetNextDelay(Options.ReconnectBackoffMultiplier, Options.MaxReconnectDelay);
		state.NextDelay = delay;

		Debug.WriteLine($"Scheduling reconnect for {endpoint} in {delay.TotalSeconds:F1}s (attempt #{state.AttemptCount + 1})");

		_ = Task.Delay(delay).ContinueWith(async _ =>
		{
			if (_disposed)
				return;

			await AttemptReconnect(endpoint);
		});
	}

	private async Task AttemptReconnect(string endpoint)
	{
		if (_disposed || _connectionParameters == null)
			return;

		if (!_reconnectionStates.TryGetValue(endpoint, out var state))
			return;

		if (_connections.TryGetValue(endpoint, out var existingSocket) && existingSocket.IsConnected)
		{
			// 既に接続済み
			state.Reset();
			return;
		}

		state.MarkAttempt();

		try
		{
			Debug.WriteLine($"Attempting to reconnect to {endpoint} (attempt #{state.AttemptCount})");

			// 既存の切断されたソケットがある場合は破棄
			if (existingSocket != null)
			{
				await existingSocket.DisconnectAsync();
				existingSocket.Dispose();
			}

			// 新しいソケットを作成して接続
			var socket = new DmdataV2Socket(ApiClient);
			SetupSocketEvents(socket, endpoint);
			_connections[endpoint] = socket;

			await socket.ConnectAsync(_connectionParameters, endpoint);

			// 接続成功
			state.Reset();
			Debug.WriteLine($"Successfully reconnected to {endpoint}");

			var wasDisconnected = Status == RedundancyStatus.Disconnected;
			UpdateRedundancyStatus();

			if (wasDisconnected)
			{
				RedundancyRestored?.Invoke(this, new RedundancyRestoredEventArgs
				{
					RestoredTime = DateTime.Now,
					RestoredEndpoint = endpoint,
					TotalActiveConnections = ActiveConnectionCount
				});
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Failed to reconnect to {endpoint}: {ex.Message}");
			ConnectionError?.Invoke(this, new ConnectionErrorEventArgs
			{
				EndpointName = endpoint,
				Exception = ex
			});

			state.IsReconnecting = false;

			// 再接続失敗時は再スケジュール（最大試行回数チェック）
			if (Options.MaxReconnectAttempts < 0 || state.AttemptCount < Options.MaxReconnectAttempts)
			{
				ScheduleReconnect(endpoint);
			}
			else
			{
				Debug.WriteLine($"Max reconnect attempts reached for {endpoint}");
			}
		}
	}

	private TimeSpan GetNextReconnectDelay(string endpoint)
	{
		if (_reconnectionStates.TryGetValue(endpoint, out var state))
		{
			return state.GetNextDelay(Options.ReconnectBackoffMultiplier, Options.MaxReconnectDelay);
		}
		return Options.InitialReconnectDelay;
	}


	#endregion

	#region IDisposable

	/// <summary>
	/// オブジェクトを破棄する
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		foreach (var socket in _connections.Values)
		{
			socket.Dispose();
		}

		_connections.Clear();
		GC.SuppressFinalize(this);
	}

	#endregion

	#region Helper Classes

	private class ReconnectionState
	{
		public int AttemptCount { get; set; }
		public DateTime LastAttempt { get; set; }
		public TimeSpan NextDelay { get; set; }
		public bool IsReconnecting { get; set; }

		public ReconnectionState()
		{
			AttemptCount = 0;
			LastAttempt = DateTime.MinValue;
			NextDelay = TimeSpan.FromSeconds(1);
			IsReconnecting = false;
		}

		public TimeSpan GetNextDelay(double backoffMultiplier, TimeSpan maxDelay)
		{
			if (AttemptCount == 0)
				return TimeSpan.FromSeconds(1);

			var delay = TimeSpan.FromMilliseconds(NextDelay.TotalMilliseconds * backoffMultiplier);
			return delay > maxDelay ? maxDelay : delay;
		}

		public void MarkAttempt()
		{
			AttemptCount++;
			LastAttempt = DateTime.Now;
			IsReconnecting = true;
		}

		public void Reset()
		{
			AttemptCount = 0;
			LastAttempt = DateTime.MinValue;
			NextDelay = TimeSpan.FromSeconds(1);
			IsReconnecting = false;
		}
	}

	#endregion
}

/// <summary>
/// 冗長性WebSocketの設定オプション
/// </summary>
public class RedundantSocketOptions
{
	/// <summary>
	/// デフォルトエンドポイント
	/// </summary>
	public static readonly string[] DefaultEndpoints = [
		DmdataV2SocketEndpoints.Tokyo,
		DmdataV2SocketEndpoints.Osaka,
	];

	/// <summary>
	/// 重複排除キャッシュサイズ
	/// </summary>
	public int DeduplicationCacheSize { get; set; } = 1000;

	/// <summary>
	/// 生データイベントを有効にするかどうか
	/// </summary>
	public bool EnableRawDataEvents { get; set; } = true;

	/// <summary>
	/// 初期再接続試行間隔
	/// </summary>
	public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// 最大再接続試行間隔
	/// </summary>
	public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(60);

	/// <summary>
	/// 再接続バックオフ倍率
	/// </summary>
	public double ReconnectBackoffMultiplier { get; set; } = 2.0;

	/// <summary>
	/// 最大再接続試行回数（-1で無制限）
	/// </summary>
	public int MaxReconnectAttempts { get; set; } = -1;
}
