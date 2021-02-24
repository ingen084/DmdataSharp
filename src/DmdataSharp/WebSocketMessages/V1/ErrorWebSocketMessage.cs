using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V1
{
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
		public string? Error { get; set; }
		/// <summary>
		/// エラーコード
		/// </summary>
		[JsonPropertyName("code")]
		public string? Code { get; set; }
		/// <summary>
		/// 行動内容
		/// <para>closeの場合は切断される</para>
		/// </summary>
		[JsonPropertyName("action")]
		public string? Action { get; set; }
	}
}
