using DmdataSharp.ApiParameters.V2;
using DmdataSharp.Redundancy;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Threading.Tasks;

namespace DmdataSharp.Interfaces
{
	/// <summary>
	/// 複数のdmdata WebSocket接続を管理する冗長性コントローラーのインターフェイス
	/// </summary>
	public interface IRedundantDmdataSocketController : IAsyncDisposable, IDisposable
	{
		/// <summary>
		/// 受信した総メッセージ数
		/// </summary>
		long TotalMessagesReceived { get; }

		/// <summary>
		/// フィルタされた重複メッセージ数
		/// </summary>
		long DuplicateMessagesFiltered { get; }

		/// <summary>
		/// 最後にメッセージを受信した時刻
		/// </summary>
		DateTime? LastMessageTime { get; }

		/// <summary>
		/// APIクライアント
		/// </summary>
		IDmdataV2ApiClient ApiClient { get; }

		/// <summary>
		/// オプション設定
		/// </summary>
		RedundantSocketOptions Options { get; }

		/// <summary>
		/// 少なくとも1つの接続が有効かどうか
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// 現在のアクティブ接続数
		/// </summary>
		int ActiveConnectionCount { get; }

		/// <summary>
		/// 接続されているエンドポイント名の配列
		/// </summary>
		string[] ConnectedEndpoints { get; }

		/// <summary>
		/// 現在の冗長性状態
		/// </summary>
		RedundancyStatus Status { get; }

		/// <summary>
		/// 新規データ受信
		/// </summary>
		event EventHandler<DataWebSocketMessage>? DataReceived;

		/// <summary>
		/// 重複排除前のデータ受信
		/// </summary>
		event EventHandler<RawDataReceivedEventArgs>? RawDataReceived;

		/// <summary>
		/// 個別接続が確立された
		/// </summary>
		event EventHandler<ConnectionEstablishedEventArgs>? ConnectionEstablished;

		/// <summary>
		/// 個別接続が失われた
		/// </summary>
		event EventHandler<ConnectionLostEventArgs>? ConnectionLost;

		/// <summary>
		/// すべての接続が失われた
		/// </summary>
		event EventHandler<AllConnectionsLostEventArgs>? AllConnectionsLost;

		/// <summary>
		/// 冗長性が復旧した（切断状態から少なくとも1つの接続が復旧）
		/// </summary>
		event EventHandler<RedundancyRestoredEventArgs>? RedundancyRestored;

		/// <summary>
		/// 接続でエラーが発生した
		/// </summary>
		event EventHandler<ConnectionErrorEventArgs>? ConnectionError;

		/// <summary>
		/// 冗長性状態が変更された
		/// </summary>
		event EventHandler<RedundancyStatusChangedEventArgs>? RedundancyStatusChanged;

		/// <summary>
		/// 複数のエンドポイントに接続
		/// </summary>
		Task ConnectAsync(SocketStartRequestParameter param, string[]? endpoints = null);

		/// <summary>
		/// すべての接続を切断
		/// </summary>
		Task DisconnectAsync();

		/// <summary>
		/// 特定のエンドポイントを再接続
		/// </summary>
		/// <param name="endpoint">再接続するエンドポイント名</param>
		Task ReconnectEndpointAsync(string endpoint);
	}
}