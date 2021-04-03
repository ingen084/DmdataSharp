using DmdataSharp.Exceptions;
using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
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
			PingId = ping?.PingId ?? throw new DmdataException("pingが正常に受信できませんでした");
		}

		/// <summary>
		/// PONGのID
		/// </summary>
		[JsonPropertyName("pingId")]
		public string PingId { get; set; }
	}
}
