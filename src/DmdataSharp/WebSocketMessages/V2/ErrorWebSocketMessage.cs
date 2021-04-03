using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// WebSocketから飛んでくるerrorメッセージを表す
	/// </summary>
	public class ErrorWebSocketMessage : DmdataWebSocketMessage
	{
		/// <summary>
		/// dataメッセージを初期化する
		/// </summary>
		public ErrorWebSocketMessage()
		{
			Type = "error";
		}

		/// <summary>
		/// エラー内容
		/// </summary>
		[JsonPropertyName("error")]
		public string Error { get; set; }
		/// <summary>
		/// エラーコード
		/// </summary>
		[JsonPropertyName("code")]
		public string Code { get; set; }
		/// <summary>
		/// 行動内容
		/// <para>trueの場合は切断される</para>
		/// </summary>
		[JsonPropertyName("close")]
		public bool Close { get; set; }
	}
}
