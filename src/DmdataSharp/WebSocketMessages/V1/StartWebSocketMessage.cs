using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V1
{
	/// <summary>
	/// WebSocketから飛んでくるstartメッセージを表す
	/// </summary>
	public class StartWebSocketMessage : DmdataWebSocketMessage
	{
		/// <summary>
		/// dataメッセージを初期化する
		/// </summary>
		public StartWebSocketMessage()
		{
			Type = "start";
		}
		/// <summary>
		/// 受信する区分
		/// </summary>
		[JsonPropertyName("classification")]
		public string[]? Classification { get; set; }
		/// <summary>
		/// 現時刻
		/// </summary>
		[JsonPropertyName("time")]
		public DateTime? Time { get; set; }
	}
}
