using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiParameters.V2
{
	/// <summary>
	/// WebSocket V2 を開始するためのパラメータ
	/// </summary>
	public class SocketStartRequestParameter
	{
		/// <summary>
		/// WebSocket V2 を開始するためのパラメータを初期化する
		/// </summary>
		/// <param name="classifications">受信する配信区分</param>
		public SocketStartRequestParameter(string[] classifications)
		{
			Classifications = classifications ?? throw new ArgumentNullException(nameof(classifications));
		}
		/// <summary>
		/// WebSocket V2 を開始するためのパラメータを初期化する
		/// </summary>
		/// <param name="classifications">受信する配信区分</param>
		public SocketStartRequestParameter(params TelegramCategoryV1[] classifications)
		{
			Classifications = classifications.Select(g => g.ToParameterString()).ToArray();
		}

		/// <summary>
		/// WebSocketで取得する配信区分
		/// </summary>
		[JsonPropertyName("classifications")]
		public string[] Classifications { get; set; }
		/// <summary>
		/// 取得したい電文種類コード
		/// </summary>
		[JsonPropertyName("types")]
		public string[]? Types { get; set; }
		/// <summary>
		/// テスト電文を受け取るか
		/// <para>受け取る場合は including にする</para>
		/// <para>注意：XML電文以外のテスト配信は no 時も配信されます。</para>
		/// </summary>
		[JsonPropertyName("test")]
		public string? Test { get; set; }
		/// <summary>
		/// アプリケーション名
		/// <para>最大24バイトまで</para>
		/// </summary>
		[JsonPropertyName("appName")]
		public string? AppName { get; set; }
		/// <summary>
		/// データフォーマットの指定
		/// <para>生電文: raw、JSON化データ: json</para>
		/// </summary>
		[JsonPropertyName("formatMode")]
		public string FormatMode { get; set; } = "raw";
	}
}
