using DmdataSharp.ApiParameters.V2;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DmdataSharp.Redundancy;

/// <summary>
/// 複数のdmdata WebSocket接続を管理する冗長性コントローラー
/// </summary>
public class RedundantDmdataSocketController : IDisposable
{
	private readonly DmdataV2ApiClient _apiClient;
	private readonly MessageDeduplicator _deduplicator;
	private readonly ConcurrentDictionary<string, ReconnectableDmdataSocket> _connections = new();
	private readonly RedundantSocketOptions _options;
	private readonly object _statusLock = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	private SocketStartRequestParameter? _currentParameters;
	private RedundancyStatus _status = RedundancyStatus.Disconnected;
	private bool _disposed;

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
	/// 冗長性dmdata WebSocketコントローラーを初期化する
	/// </summary>
	/// <param name="apiClient">dmdata APIクライアント</param>
	/// <param name="options">オプション設定（省略時はデフォルト値を使用）</param>
	public RedundantDmdataSocketController(DmdataV2ApiClient apiClient, RedundantSocketOptions? options = null)
	{
		_apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
		_options = options ?? new RedundantSocketOptions();
		_deduplicator = new MessageDeduplicator(_options.DeduplicationCacheSize);
	}

	#region Properties

	/// <summary>
	/// APIクライアント
	/// </summary>
	public DmdataV2ApiClient ApiClient => _apiClient;

	/// <summary>
	/// オプション設定
	/// </summary>
	public RedundantSocketOptions Options => _options;

	/// <summary>
	/// 少なくとも1つの接続が有効かどうか
	/// </summary>
	public bool IsConnected => _connections.Values.Any(c => c.IsConnected);

	/// <summary>
	/// 現在のアクティブ接続数
	/// </summary>
	public int ActiveConnectionCount => _connections.Values.Count(c => c.IsConnected);

	/// <summary>
	/// 接続されているエンドポイント名の配列
	/// </summary>
	public string[] ConnectedEndpoints => [.. _connections.Where(kv => kv.Value.IsConnected).Select(kv => kv.Key)];

	/// <summary>
	/// 現在の冗長性状態
	/// </summary>
	public RedundancyStatus Status => _status;

	#endregion

	#region Events

	/// <summary>
	/// 重複排除後のデータが受信された
	/// </summary>
	public event EventHandler<DataWebSocketMessage>? DataReceived;

	/// <summary>
	/// 重複排除前の生データが受信された
	/// </summary>
	public event EventHandler<RawDataReceivedEventArgs>? RawDataReceived;

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

	#region Public Methods

	/// <summary>
	/// 複数のエンドポイントに接続
	/// </summary>
	public async Task ConnectAsync(SocketStartRequestParameter param, string[]? endpoints = null)
	{
		if (_disposed) throw new ObjectDisposedException(nameof(RedundantDmdataSocketController));

		// 既存の接続があれば切断
		if (_connections.Count > 0)
		{
			await DisconnectAsync();
		}

		_currentParameters = param;
		var targetEndpoints = endpoints ?? RedundantSocketOptions.DefaultEndpoints;
		var wasDisconnected = !IsConnected;

		// 各エンドポイントに接続
		var connectTasks = targetEndpoints.Select(endpoint => ConnectToEndpointAsync(endpoint, param));
		await Task.WhenAll(connectTasks);

		UpdateStatus();

		// イベント発火
		if (wasDisconnected && IsConnected)
		{
			var firstConnected = ConnectedEndpoints.FirstOrDefault() ?? string.Empty;
			RedundancyRestored?.Invoke(this, new RedundancyRestoredEventArgs
			{
				RestoredTime = DateTime.Now,
				RestoredEndpoint = firstConnected,
				TotalActiveConnections = ActiveConnectionCount
			});
		}
		else if (!IsConnected)
		{
			AllConnectionsLost?.Invoke(this, new AllConnectionsLostEventArgs
			{
				LostTime = DateTime.Now,
				DisconnectedEndpoints = targetEndpoints,
				WillAttemptReconnect = true,
				NextReconnectAttempt = TimeSpan.FromSeconds(1)
			});
		}
	}

	/// <summary>
	/// すべての接続を切断
	/// </summary>
	public async Task DisconnectAsync()
	{
		if (_disposed) return;

		// キャンセルトークンをキャンセル
		_cancellationTokenSource.Cancel();

		// 接続を切断
		var disconnectTasks = _connections.Values.Select(c => c.DisconnectAsync());
		await Task.WhenAll(disconnectTasks);

		// クリーンアップ
		foreach (var kvp in _connections.ToArray())
		{
			if (_connections.TryRemove(kvp.Key, out var connection))
			{
				connection.Dispose();
			}
		}


		UpdateStatus();
	}

	/// <summary>
	/// 特定のエンドポイントを再接続
	/// </summary>
	/// <param name="endpoint">再接続するエンドポイント名</param>
	public async Task ReconnectEndpointAsync(string endpoint)
	{
		if (_disposed) throw new ObjectDisposedException(nameof(RedundantDmdataSocketController));
		if (_currentParameters == null) throw new InvalidOperationException("No connection parameters available");

		if (_connections.TryGetValue(endpoint, out var connection))
		{
			await connection.ConnectAsync(_currentParameters);
		}
	}

	#endregion

	#region Private Methods

	private async Task ConnectToEndpointAsync(string endpoint, SocketStartRequestParameter param)
	{
		try
		{
			// 既存の接続があれば破棄
			if (_connections.TryRemove(endpoint, out var oldConnection))
			{
				oldConnection.Dispose();
			}

			// 新しい接続を作成
			var connection = new ReconnectableDmdataSocket(_apiClient, endpoint, new ReconnectionOptions
			{
				InitialDelay = _options.InitialReconnectDelay,
				MaxDelay = _options.MaxReconnectDelay,
				BackoffMultiplier = _options.ReconnectBackoffMultiplier,
				MaxAttempts = _options.MaxReconnectAttempts
			});

			// イベントハンドラを設定
			SetupConnectionEvents(connection, endpoint);
			
			// キャンセルトークンを設定
			connection.SetCancellationToken(_cancellationTokenSource.Token);

			_connections[endpoint] = connection;

			// 接続を試行
			await connection.ConnectAsync(param);
		}
		catch (Exception ex)
		{
			ConnectionError?.Invoke(this, new ConnectionErrorEventArgs
			{
				EndpointName = endpoint,
				Exception = ex
			});
		}
	}

	private void SetupConnectionEvents(ReconnectableDmdataSocket connection, string endpoint)
	{
		connection.Connected += (s, e) =>
		{
			ConnectionEstablished?.Invoke(this, new ConnectionEstablishedEventArgs
			{
				EndpointName = endpoint,
				StartMessage = e,
				ConnectedTime = DateTime.Now
			});
			UpdateStatus();
		};

		connection.DataReceived += (s, e) =>
		{
			if (e == null) return;

			TotalMessagesReceived++;
			LastMessageTime = DateTime.Now;

			var isDuplicate = _deduplicator.IsMessageDuplicate(e.Id);

			// 生データイベント
			if (_options.EnableRawDataEvents)
			{
				RawDataReceived?.Invoke(this, new RawDataReceivedEventArgs
				{
					EndpointName = endpoint,
					Message = e,
					IsDuplicate = isDuplicate,
					ReceivedTime = DateTime.Now
				});
			}

			// 重複でない場合のみデータイベントを発火
			if (!isDuplicate)
			{
				DataReceived?.Invoke(this, e);
			}
			else
			{
				DuplicateMessagesFiltered++;
			}
		};

		connection.Disconnected += (s, e) =>
		{
			ConnectionLost?.Invoke(this, new ConnectionLostEventArgs
			{
				EndpointName = endpoint,
				DisconnectedTime = DateTime.Now,
				WillReconnect = true,
				Reason = "Connection lost"
			});

			var wasConnected = Status != RedundancyStatus.Disconnected;
			UpdateStatus();

			if (wasConnected && Status == RedundancyStatus.Disconnected)
			{
				AllConnectionsLost?.Invoke(this, new AllConnectionsLostEventArgs
				{
					LostTime = DateTime.Now,
					DisconnectedEndpoints = [.. _connections.Keys],
					WillAttemptReconnect = true,
					NextReconnectAttempt = TimeSpan.FromSeconds(1)
				});
			}
		};

		connection.Error += (s, e) =>
		{
			ConnectionError?.Invoke(this, new ConnectionErrorEventArgs
			{
				EndpointName = endpoint,
				ErrorMessage = e
			});
		};
		
		// 再接続イベントを設定
		connection.ReconnectionAttempt += (s, e) =>
		{
			// 再接続試行のログ出力などが必要な場合はここで処理
		};
		
		connection.ReconnectionSucceeded += (s, e) =>
		{
			var wasDisconnected = Status == RedundancyStatus.Disconnected;
			UpdateStatus();

			if (wasDisconnected)
			{
				RedundancyRestored?.Invoke(this, new RedundancyRestoredEventArgs
				{
					RestoredTime = DateTime.Now,
					RestoredEndpoint = endpoint,
					TotalActiveConnections = ActiveConnectionCount
				});
			}
		};
		
		connection.ReconnectionFailed += (s, e) =>
		{
			// 再接続失敗のログ出力などが必要な場合はここで処理
		};
	}


	private void UpdateStatus()
	{
		var activeCount = ActiveConnectionCount;
		var totalCount = _connections.Count;
		RedundancyStatus newStatus;

		if (activeCount == 0)
		{
			newStatus = RedundancyStatus.Disconnected;
		}
		else if (activeCount == totalCount)
		{
			newStatus = RedundancyStatus.FullyConnected;
		}
		else if (activeCount >= totalCount / 2)
		{
			newStatus = RedundancyStatus.PartiallyConnected;
		}
		else
		{
			newStatus = RedundancyStatus.Degraded;
		}

		lock (_statusLock)
		{
			if (newStatus != _status)
			{
				_status = newStatus;
				RedundancyStatusChanged?.Invoke(this, new RedundancyStatusChangedEventArgs
				{
					Status = newStatus,
					ActiveConnections = activeCount,
					ActiveEndpoints = ConnectedEndpoints,
					ChangedTime = DateTime.Now
				});
			}
		}
	}

	#endregion

	#region IDisposable

	/// <summary>
	/// リソースを解放する
	/// </summary>
	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_cancellationTokenSource.Cancel();
		
		// すべての接続を切断
		try
		{
			DisconnectAsync().Wait(TimeSpan.FromSeconds(5));
		}
		catch
		{
			// 強制破棄
			foreach (var connection in _connections.Values)
			{
				connection.Dispose();
			}
		}

		_deduplicator.Clear();
		_cancellationTokenSource.Dispose();
		GC.SuppressFinalize(this);
	}

	#endregion
}
