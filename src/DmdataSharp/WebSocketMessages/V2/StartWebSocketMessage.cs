using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// WebSocketから飛んでくるstartメッセージを表す
	/// </summary>
	public class StartWebSocketMessage : DmdataWebSocketMessage
	{
		/// <summary>
		/// dataメッセージを初期化する
		/// </summary>
		public StartWebSocketMessage()
		{
			Type = "start";
		}

		/// <summary>
		/// ソケットID
		/// </summary>
		[JsonPropertyName("socketId")]
		public int SocketId { get; set; }
		/// <summary>
		/// 受け取る配信区分
		/// </summary>
		[JsonPropertyName("classifications")]
		public string[] Classifications { get; set; }
		/// <summary>
		/// including の時のみ、XML電文のテストをWebsocketで受け取る
		/// </summary>
		[JsonPropertyName("test")]
		public string Test { get; set; }
		/// <summary>
		/// WebSocketで受け取るデータ種類コードリスト。Null時は受け取る配信区分の全部を受け取る
		/// </summary>
		[JsonPropertyName("types")]
		public string[]? Types { get; set; }
		/// <summary>
		/// WebSocketで受け取る情報フォーマット
		/// </summary>
		[JsonPropertyName("formats")]
		public string[] Formats { get; set; }
		/// <summary>
		/// リクエストで指定したアプリ名
		/// </summary>
		[JsonPropertyName("appName")]
		public string? AppName { get; set; }
		/// <summary>
		/// 現時刻
		/// </summary>
		[JsonPropertyName("time")]
		public DateTime? Time { get; set; }
	}
}
