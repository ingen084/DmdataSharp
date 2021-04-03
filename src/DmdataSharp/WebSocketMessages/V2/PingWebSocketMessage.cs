using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// WebSocketから飛んでくるpingメッセージを表す
	/// </summary>
	public class PingWebSocketMessage : DmdataWebSocketMessage
	{
		/// <summary>
		/// pingメッセージを初期化する
		/// </summary>
		public PingWebSocketMessage()
		{
			Type = "ping";
		}

		/// <summary>
		/// pingidを指定しpingメッセージを初期化する
		/// </summary>
		public PingWebSocketMessage(string pingId)
		{
			Type = "ping";
			PingId = pingId;
		}

		/// <summary>
		/// PINGのID
		/// </summary>
		[JsonPropertyName("pingId")]
		public string PingId { get; set; }
	}
}
