using System;

namespace DmdataSharp.Redundancy;

/// <summary>
/// 再接続オプション設定
/// </summary>
public class ReconnectionOptions
{
	/// <summary>
	/// 初期遅延時間
	/// </summary>
	public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
	
	/// <summary>
	/// 最大遅延時間
	/// </summary>
	public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(60);
	
	/// <summary>
	/// バックオフ倍率
	/// </summary>
	public double BackoffMultiplier { get; set; } = 2.0;
	
	/// <summary>
	/// 最大試行回数（-1で無制限）
	/// </summary>
	public int MaxAttempts { get; set; } = -1; // -1 = unlimited
}

/// <summary>
/// 再接続試行イベント引数
/// </summary>
public class ReconnectionAttemptEventArgs : EventArgs
{
	/// <summary>
	/// エンドポイント名
	/// </summary>
	public string Endpoint { get; }
	
	/// <summary>
	/// 試行番号
	/// </summary>
	public int AttemptNumber { get; }
	
	/// <summary>
	/// 遅延時間
	/// </summary>
	public TimeSpan Delay { get; }

	/// <summary>
	/// 再接続試行イベント引数を初期化する
	/// </summary>
	/// <param name="endpoint">エンドポイント名</param>
	/// <param name="attemptNumber">試行番号</param>
	/// <param name="delay">遅延時間</param>
	public ReconnectionAttemptEventArgs(string endpoint, int attemptNumber, TimeSpan delay)
	{
		Endpoint = endpoint;
		AttemptNumber = attemptNumber;
		Delay = delay;
	}
}

/// <summary>
/// 再接続成功イベント引数
/// </summary>
public class ReconnectionSucceededEventArgs : EventArgs
{
	/// <summary>
	/// エンドポイント名
	/// </summary>
	public string Endpoint { get; }

	/// <summary>
	/// 再接続成功イベント引数を初期化する
	/// </summary>
	/// <param name="endpoint">エンドポイント名</param>
	public ReconnectionSucceededEventArgs(string endpoint)
	{
		Endpoint = endpoint;
	}
}

/// <summary>
/// 再接続失敗イベント引数
/// </summary>
public class ReconnectionFailedEventArgs : EventArgs
{
	/// <summary>
	/// エンドポイント名
	/// </summary>
	public string Endpoint { get; }
	
	/// <summary>
	/// 試行番号
	/// </summary>
	public int AttemptNumber { get; }
	
	/// <summary>
	/// 失敗理由
	/// </summary>
	public string Reason { get; }

	/// <summary>
	/// 再接続失敗イベント引数を初期化する
	/// </summary>
	/// <param name="endpoint">エンドポイント名</param>
	/// <param name="attemptNumber">試行番号</param>
	/// <param name="reason">失敗理由</param>
	public ReconnectionFailedEventArgs(string endpoint, int attemptNumber, string reason)
	{
		Endpoint = endpoint;
		AttemptNumber = attemptNumber;
		Reason = reason;
	}
}