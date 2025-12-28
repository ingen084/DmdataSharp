using DmdataSharp.ApiParameters.V2;
using DmdataSharp.WebSocketMessages.V2;
using System;
using System.Threading.Tasks;

namespace DmdataSharp.Interfaces
{
	/// <summary>
	/// dmdataのWebSocket V2セッションのインターフェイス
	/// </summary>
	public interface IDmdataV2Socket : IDisposable
	{
		/// <summary>
		/// WebSocketへの接続が完了した
		/// </summary>
		event EventHandler<StartWebSocketMessage?>? Connected;

		/// <summary>
		/// errorメッセージが飛んできた
		/// </summary>
		event EventHandler<ErrorWebSocketMessage?>? Error;

		/// <summary>
		/// WebSocketが切断された
		/// </summary>
		event EventHandler<EventArgs?>? Disconnected;

		/// <summary>
		/// dataメッセージが飛んできた
		/// </summary>
		event EventHandler<DataWebSocketMessage?>? DataReceived;

		/// <summary>
		/// WebSocketに接続中かどうか
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// 親となるAPIクライアント
		/// </summary>
		IDmdataV2ApiClient ApiClient { get; }

		/// <summary>
		/// WebSocketが切断済みかどうかを取得する
		/// </summary>
		bool IsDisposed { get; }

		/// <summary>
		/// WebSocketに接続する
		/// </summary>
		/// <param name="param">ソケット開始</param>
		/// <param name="customHostName">地理冗長化のためなどに接続先のホスト名をカスタムする場合</param>
		/// <returns></returns>
		Task ConnectAsync(SocketStartRequestParameter param, string? customHostName = null);

		/// <summary>
		/// WebSocketに接続する
		/// </summary>
		/// <param name="uri">接続先のURL</param>
		/// <returns></returns>
		Task ConnectAsync(Uri uri);

		/// <summary>
		/// WebSocketから切断する
		/// </summary>
		/// <returns></returns>
		Task DisconnectAsync();
	}
}