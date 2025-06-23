using DmdataSharp.WebSocketMessages.V2;
using System;

namespace DmdataSharp.Redundancy;

/// <summary>
/// 重複排除前の生データ受信イベント引数
/// </summary>
public class RawDataReceivedEventArgs : EventArgs
{
	/// <summary>
	/// データを受信したエンドポイント名
	/// </summary>
	public string EndpointName { get; set; } = string.Empty;

	/// <summary>
	/// 受信したメッセージ
	/// </summary>
	public DataWebSocketMessage? Message { get; set; }

	/// <summary>
	/// このメッセージが重複かどうか
	/// </summary>
	public bool IsDuplicate { get; set; }

	/// <summary>
	/// 受信時刻
	/// </summary>
	public DateTime ReceivedTime { get; set; }
}

/// <summary>
/// 接続確立イベント引数
/// </summary>
public class ConnectionEstablishedEventArgs : EventArgs
{
	/// <summary>
	/// 接続されたエンドポイント名
	/// </summary>
	public string EndpointName { get; set; } = string.Empty;

	/// <summary>
	/// 接続開始メッセージ
	/// </summary>
	public StartWebSocketMessage? StartMessage { get; set; }

	/// <summary>
	/// 接続時刻
	/// </summary>
	public DateTime ConnectedTime { get; set; }
}

/// <summary>
/// 接続失われイベント引数
/// </summary>
public class ConnectionLostEventArgs : EventArgs
{
	/// <summary>
	/// 切断されたエンドポイント名
	/// </summary>
	public string EndpointName { get; set; } = string.Empty;

	/// <summary>
	/// 切断理由
	/// </summary>
	public string Reason { get; set; } = string.Empty;

	/// <summary>
	/// 再接続を試行するかどうか
	/// </summary>
	public bool WillReconnect { get; set; }

	/// <summary>
	/// 切断時刻
	/// </summary>
	public DateTime DisconnectedTime { get; set; }
}

/// <summary>
/// 全接続失われイベント引数
/// </summary>
public class AllConnectionsLostEventArgs : EventArgs
{
	/// <summary>
	/// 全接続が失われた時刻
	/// </summary>
	public DateTime LostTime { get; set; }

	/// <summary>
	/// 切断されたエンドポイント一覧
	/// </summary>
	public string[] DisconnectedEndpoints { get; set; } = Array.Empty<string>();

	/// <summary>
	/// 再接続を試行するかどうか
	/// </summary>
	public bool WillAttemptReconnect { get; set; }

	/// <summary>
	/// 次回再接続試行までの時間
	/// </summary>
	public TimeSpan NextReconnectAttempt { get; set; }
}

/// <summary>
/// 冗長性復旧イベント引数
/// </summary>
public class RedundancyRestoredEventArgs : EventArgs
{
	/// <summary>
	/// 復旧時刻
	/// </summary>
	public DateTime RestoredTime { get; set; }

	/// <summary>
	/// 復旧したエンドポイント名
	/// </summary>
	public string RestoredEndpoint { get; set; } = string.Empty;

	/// <summary>
	/// 現在のアクティブ接続数
	/// </summary>
	public int TotalActiveConnections { get; set; }
}

/// <summary>
/// 接続エラーイベント引数
/// </summary>
public class ConnectionErrorEventArgs : EventArgs
{
	/// <summary>
	/// エラーが発生したエンドポイント名
	/// </summary>
	public string EndpointName { get; set; } = string.Empty;

	/// <summary>
	/// WebSocketエラーメッセージ
	/// </summary>
	public ErrorWebSocketMessage? ErrorMessage { get; set; }

	/// <summary>
	/// 発生した例外
	/// </summary>
	public Exception? Exception { get; set; }
}

/// <summary>
/// 冗長性状態変更イベント引数
/// </summary>
public class RedundancyStatusChangedEventArgs : EventArgs
{
	/// <summary>
	/// 新しい冗長性状態
	/// </summary>
	public RedundancyStatus Status { get; set; }

	/// <summary>
	/// 現在のアクティブ接続数
	/// </summary>
	public int ActiveConnections { get; set; }

	/// <summary>
	/// アクティブなエンドポイント一覧
	/// </summary>
	public string[] ActiveEndpoints { get; set; } = Array.Empty<string>();

	/// <summary>
	/// 状態変更時刻
	/// </summary>
	public DateTime ChangedTime { get; set; }
}
