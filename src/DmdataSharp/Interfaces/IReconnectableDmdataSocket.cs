using DmdataSharp.ApiParameters.V2;
using DmdataSharp.Redundancy;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DmdataSharp.Interfaces
{
	/// <summary>
	/// 自動再接続機能付きのdmdata WebSocket接続クラスのインターフェイス
	/// </summary>
	public interface IReconnectableDmdataSocket : IDisposable
	{
		/// <summary>
		/// エンドポイント名
		/// </summary>
		string Endpoint { get; }

		/// <summary>
		/// 接続されているかどうか
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// 接続が確立されたときに発生するイベント
		/// </summary>
		event EventHandler<StartWebSocketMessage>? Connected;

		/// <summary>
		/// データが受信されたときに発生するイベント
		/// </summary>
		event EventHandler<DataWebSocketMessage>? DataReceived;

		/// <summary>
		/// 接続が切断されたときに発生するイベント
		/// </summary>
		event EventHandler<EventArgs?>? Disconnected;

		/// <summary>
		/// エラーが発生したときに発生するイベント
		/// </summary>
		event EventHandler<ErrorWebSocketMessage?>? Error;

		/// <summary>
		/// 再接続を試行するときに発生するイベント
		/// </summary>
		event EventHandler<ReconnectionAttemptEventArgs>? ReconnectionAttempt;

		/// <summary>
		/// 再接続が成功したときに発生するイベント
		/// </summary>
		event EventHandler<ReconnectionSucceededEventArgs>? ReconnectionSucceeded;

		/// <summary>
		/// 再接続が失敗したときに発生するイベント
		/// </summary>
		event EventHandler<ReconnectionFailedEventArgs>? ReconnectionFailed;

		/// <summary>
		/// 外部のキャンセルトークンを設定する
		/// </summary>
		/// <param name="cancellationToken">外部のキャンセルトークン</param>
		void SetCancellationToken(CancellationToken cancellationToken);

		/// <summary>
		/// 接続を開始
		/// </summary>
		Task ConnectAsync(SocketStartRequestParameter param, CancellationToken cancellationToken = default);

		/// <summary>
		/// 接続を切断
		/// </summary>
		Task DisconnectAsync();
	}
}