using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// WebSocketに関するリスト
	/// </summary>
	public class SocketListResponse : DmdataResponse
	{
		/// <summary>
		/// アイテムリスト
		/// </summary>
		[JsonPropertyName("items")]
		public Item[] Items { get; set; }
		/// <summary>
		/// 次のリソースがある場合、取得するためのトークン
		/// </summary>
		[JsonPropertyName("nextToken")]
		public string? NextToken { get; set; }

		/// <summary>
		/// アイテムリスト
		/// </summary>
		public class Item
		{
			/// <summary>
			/// WebSocket ID
			/// </summary>
			[JsonPropertyName("id")]
			public int Id { get; set; }
			/// <summary>
			/// WebSocketに接続するためのticket
			/// </summary>
			[JsonPropertyName("ticket")]
			public string Ticket { get; set; }
			/// <summary>
			/// WebSocketで受け取るデータ種類コードリスト。Null時は受け取る配信区分の全部を受け取る
			/// </summary>
			[JsonPropertyName("types")]
			public string[]? Types { get; set; }
			/// <summary>
			/// including の時のみ、XML電文のテストをWebsocketで受け取る
			/// </summary>
			[JsonPropertyName("test")]
			public string Test { get; set; }
			/// <summary>
			/// WebSocketで受け取る配信区分
			/// </summary>
			[JsonPropertyName("classifications")]
			public string[] Classifications { get; set; }
			/// <summary>
			/// 接続IPアドレス
			/// </summary>
			[JsonPropertyName("ipAddress")]
			public string IpAddress { get; set; }
			/// <summary>
			/// 接続待機・期限切れ: waiting、接続中: open、接続終了: closed。
			/// </summary>
			[JsonPropertyName("status")]
			public string Status { get; set; }
			/// <summary>
			/// 接続先のWebSocketサーバー名
			/// </summary>
			[JsonPropertyName("server")]
			public string? Server { get; set; }
			/// <summary>
			/// 作成時間、または接続開始時間
			/// </summary>
			[JsonPropertyName("start")]
			public DateTime Start { get; set; }
			/// <summary>
			/// 接続終了時間
			/// </summary>
			[JsonPropertyName("end")]
			public DateTime? End { get; set; }
			/// <summary>
			/// Ping-Pongチェック時間
			/// </summary>
			[JsonPropertyName("ping")]
			public DateTime? Ping { get; set; }
			/// <summary>
			/// アプリ名
			/// </summary>
			[JsonPropertyName("appName")]
			public string? AppName { get; set; }
		}

	}

	[JsonSerializable(typeof(SocketListResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class SocketListResponseSerializerContext : JsonSerializerContext
	{
	}
}
