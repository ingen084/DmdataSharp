using System;

namespace DmdataSharp.Redundancy;

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