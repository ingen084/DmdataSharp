using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages
{
	/// <summary>
	/// WebSocketのメッセージ
	/// </summary>
	public class DmdataWebSocketMessage
	{
		/// <summary>
		/// メッセージの種類
		/// </summary>
		[JsonPropertyName("type")]
		public string? Type { get; set; }
	}
}
