using DmdataSharp.ApiParameters.V2;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DmdataSharp.Redundancy;

/// <summary>
/// 自動再接続機能付きのdmdata WebSocket接続クラス
/// </summary>
public class ReconnectableDmdataSocket : IDisposable
{
	private readonly DmdataV2Socket _socket;
	private readonly string _endpoint;
	private readonly ReconnectionOptions _reconnectionOptions;
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	
	private SocketStartRequestParameter? _connectionParameters;
	private CancellationToken? _externalCancellationToken;
	private int _attemptCount;
	private bool _isReconnecting;
	private bool _disposed;

	/// <summary>
	/// 再接続可能なdmdata WebSocket接続を初期化する
	/// </summary>
	/// <param name="apiClient">APIクライアント</param>
	/// <param name="endpoint">接続先エンドポイント</param>
	/// <param name="reconnectionOptions">再接続オプション</param>
	public ReconnectableDmdataSocket(DmdataV2ApiClient apiClient, string endpoint, ReconnectionOptions? reconnectionOptions = null)
	{
		_socket = new DmdataV2Socket(apiClient);
		_endpoint = endpoint;
		_reconnectionOptions = reconnectionOptions ?? new ReconnectionOptions();

		// イベントを直接転送
		_socket.Connected += (s, e) => Connected?.Invoke(this, e!);
		_socket.DataReceived += (s, e) => DataReceived?.Invoke(this, e!);
		_socket.Disconnected += OnDisconnected;
		_socket.Error += OnError;
	}

	/// <summary>
	/// エンドポイント名
	/// </summary>
	public string Endpoint => _endpoint;

	/// <summary>
	/// 接続されているかどうか
	/// </summary>
	public bool IsConnected => _socket.IsConnected;

	/// <summary>
	/// 接続が確立されたときに発生するイベント
	/// </summary>
	public event EventHandler<StartWebSocketMessage>? Connected;

	/// <summary>
	/// データが受信されたときに発生するイベント
	/// </summary>
	public event EventHandler<DataWebSocketMessage>? DataReceived;

	/// <summary>
	/// 接続が切断されたときに発生するイベント
	/// </summary>
	public event EventHandler<EventArgs?>? Disconnected;

	/// <summary>
	/// エラーが発生したときに発生するイベント
	/// </summary>
	public event EventHandler<ErrorWebSocketMessage?>? Error;

	/// <summary>
	/// 外部のキャンセルトークンを設定する
	/// </summary>
	/// <param name="cancellationToken">外部のキャンセルトークン</param>
	public void SetCancellationToken(CancellationToken cancellationToken)
	{
		_externalCancellationToken = cancellationToken;
	}

	/// <summary>
	/// 接続を開始
	/// </summary>
	public async Task ConnectAsync(SocketStartRequestParameter param, CancellationToken cancellationToken = default)
	{
		if (_disposed) throw new ObjectDisposedException(nameof(ReconnectableDmdataSocket));

		_connectionParameters = param;
		
		if (!_socket.IsConnected)
		{
			await _socket.ConnectAsync(param, _endpoint);
		}
	}

	/// <summary>
	/// 接続を切断
	/// </summary>
	public async Task DisconnectAsync()
	{
		if (_disposed) return;

		_cancellationTokenSource.Cancel();
		
		if (_socket.IsConnected)
		{
			await _socket.DisconnectAsync();
		}
	}

	private void OnDisconnected(object? sender, EventArgs? e)
	{
		Disconnected?.Invoke(this, e);
		
		if (!_disposed && !_isReconnecting)
		{
			_ = Task.Run(StartReconnectionLoop);
		}
	}

	private void OnError(object? sender, ErrorWebSocketMessage? e)
	{
		Error?.Invoke(this, e);
		
		if (!_disposed && !_isReconnecting && !_socket.IsConnected)
		{
			_ = Task.Run(StartReconnectionLoop);
		}
	}

	private async Task StartReconnectionLoop()
	{
		if (_isReconnecting || _disposed || _connectionParameters == null) return;
		
		_isReconnecting = true;
		_attemptCount = 0;

		try
		{
			while (!_socket.IsConnected && !_disposed && !_cancellationTokenSource.Token.IsCancellationRequested)
			{
				_attemptCount++;
				
				if (_reconnectionOptions.MaxAttempts > 0 && _attemptCount > _reconnectionOptions.MaxAttempts)
				{
					ReconnectionFailed?.Invoke(this, new ReconnectionFailedEventArgs(_endpoint, _attemptCount, "Max attempts reached"));
					break;
				}

				var delay = CalculateDelay();
				ReconnectionAttempt?.Invoke(this, new ReconnectionAttemptEventArgs(_endpoint, _attemptCount, delay));

				try
				{
					await Task.Delay(delay, _cancellationTokenSource.Token);
					
					var combinedToken = _externalCancellationToken.HasValue
						? CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, _externalCancellationToken.Value).Token
						: _cancellationTokenSource.Token;
					
					await _socket.ConnectAsync(_connectionParameters, _endpoint);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					ReconnectionFailed?.Invoke(this, new ReconnectionFailedEventArgs(_endpoint, _attemptCount, ex.Message));
				}
			}

			if (_socket.IsConnected)
			{
				_attemptCount = 0;
				ReconnectionSucceeded?.Invoke(this, new ReconnectionSucceededEventArgs(_endpoint));
			}
		}
		finally
		{
			_isReconnecting = false;
		}
	}

	private TimeSpan CalculateDelay()
	{
		if (_attemptCount <= 1) return _reconnectionOptions.InitialDelay;
		
		var delay = TimeSpan.FromMilliseconds(_reconnectionOptions.InitialDelay.TotalMilliseconds * Math.Pow(_reconnectionOptions.BackoffMultiplier, _attemptCount - 1));
		return delay > _reconnectionOptions.MaxDelay ? _reconnectionOptions.MaxDelay : delay;
	}

	/// <summary>
	/// 再接続を試行するときに発生するイベント
	/// </summary>
	public event EventHandler<ReconnectionAttemptEventArgs>? ReconnectionAttempt;
	
	/// <summary>
	/// 再接続が成功したときに発生するイベント
	/// </summary>
	public event EventHandler<ReconnectionSucceededEventArgs>? ReconnectionSucceeded;
	
	/// <summary>
	/// 再接続が失敗したときに発生するイベント
	/// </summary>
	public event EventHandler<ReconnectionFailedEventArgs>? ReconnectionFailed;

	/// <summary>
	/// リソースを解放する
	/// </summary>
	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;

		_cancellationTokenSource.Cancel();
		_socket.Dispose();
		_cancellationTokenSource.Dispose();
	}
}
