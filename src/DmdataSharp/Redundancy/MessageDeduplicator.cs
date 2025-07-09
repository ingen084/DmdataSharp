using System.Collections.Concurrent;

namespace DmdataSharp.Redundancy;

/// <summary>
/// メッセージの重複排除を行うクラス
/// </summary>
/// <param name="maxCacheSize">最大キャッシュサイズ（デフォルト: 1000）</param>
public class MessageDeduplicator(int maxCacheSize = 1000)
{
	private readonly ConcurrentDictionary<string, byte> _seenMessageIds = new();
	private readonly ConcurrentQueue<string> _messageQueue = new();
	private readonly int _maxCacheSize = maxCacheSize;


	/// <summary>
	/// メッセージが重複かどうかを確認し、新しいメッセージの場合はキャッシュに追加
	/// </summary>
	/// <param name="messageId">メッセージID</param>
	/// <returns>重複の場合true</returns>
	public bool IsMessageDuplicate(string messageId)
	{
		// アトミックな追加を試行
		if (!_seenMessageIds.TryAdd(messageId, 0))
			return true; // 既に存在する場合は重複

		// キューに追加
		_messageQueue.Enqueue(messageId);

		// キャッシュサイズ制限
		while (_messageQueue.Count > _maxCacheSize)
		{
			if (_messageQueue.TryDequeue(out var oldId))
			{
				_seenMessageIds.TryRemove(oldId, out _);
			}
		}

		return false;
	}

	/// <summary>
	/// キャッシュをクリア
	/// </summary>
	public void Clear()
	{
		_seenMessageIds.Clear();
		while (_messageQueue.TryDequeue(out _)) { }
	}

	/// <summary>
	/// 現在のキャッシュサイズ
	/// </summary>
	public int CacheSize => _seenMessageIds.Count;
}