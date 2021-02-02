using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages
{
	/// <summary>
	/// WebSocketから飛んでくるpongメッセージを表す
	/// </summary>
	public class PongWebSocketMessage : DmdataWebSocketMessage
	{
		/// <summary>
		/// pongメッセージを初期化する
		/// </summary>
		public PongWebSocketMessage()
		{
			Type = "pong";
		}
		/// <summary>
		/// pongメッセージをpingメッセージから初期化する
		/// </summary>
		/// <param name="ping"></param>
		public PongWebSocketMessage(PingWebSocketMessage? ping)
		{
			Type = "pong";
			PingId = ping?.PingId;
		}

		/// <summary>
		/// PONGのID
		/// </summary>
		[JsonPropertyName("pingId")]
		public string? PingId { get; set; }
	}
}
