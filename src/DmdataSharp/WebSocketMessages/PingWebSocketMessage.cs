using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages
{
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
		/// PINGのID
		/// </summary>
		[JsonPropertyName("pingId")]
		public string? PingId { get; set; }
	}
}
