using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// WebSocket v2 に接続するためのAPIリクエスト結果
	/// </summary>
	public class SocketStartResponse : DmdataResponse
	{
		/// <summary>
		/// WebSocketに接続するためのticket
		/// </summary>
		[JsonPropertyName("ticket")]
		public string Ticket { get; set; }
		/// <summary>
		/// WebSocketの接続情報
		/// </summary>
		[JsonPropertyName("websocket")]
		public Info Websocket { get; set; }
		/// <summary>
		/// WebSocketで受け取る配信区分
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
		/// WebSocketの接続情報
		/// </summary>
		public class Info
		{
			/// <summary>
			/// WebSocket ID
			/// </summary>
			[JsonPropertyName("id")]
			public int Id { get; set; }
			/// <summary>
			/// WebSocketの接続先URLでticket付き
			/// </summary>
			[JsonPropertyName("url")]
			public string Url { get; set; }
			/// <summary>
			/// WebSocketのProtocolで配列の要素は dmdata.v2 一つで固定
			/// </summary>
			[JsonPropertyName("protocol")]
			public string[] Protocol { get; set; }
			/// <summary>
			/// キーの有効時間で単位は秒。値は 300 で固定
			/// </summary>
			[JsonPropertyName("expiration")]
			public int Expiration { get; set; }
		}
	}

	[JsonSerializable(typeof(SocketStartResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class SocketStartResponseSerializerContext : JsonSerializerContext
	{
	}
}
