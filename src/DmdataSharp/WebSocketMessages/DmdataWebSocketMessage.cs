using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// WebSocketのメッセージ
	/// </summary>
	public class DmdataWebSocketMessage
	{
		/// <summary>
		/// メッセージの種類
		/// </summary>
		[JsonPropertyName("type")]
		public string Type { get; set; }
	}
}
