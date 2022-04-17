using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// 電文リストの結果
	/// </summary>
	public class TelegramListResponse : DmdataResponse
	{
		/// <summary>
		/// 電文情報リスト
		/// </summary>
		[JsonPropertyName("items")]
		public Item[] Items { get; set; }
		/// <summary>
		/// 次のリソースがある場合に使用するトークン
		/// </summary>
		[JsonPropertyName("nextToken")]
		public string NextToken { get; set; }
		/// <summary>
		/// PULL時に使用するためのトークン
		/// </summary>
		[JsonPropertyName("nextPooling")]
		public string NextPooling { get; set; }
		/// <summary>
		/// PULL時、次にリクエストするまでの待機すべき最小時間（ミリ秒）
		/// </summary>
		[JsonPropertyName("nextPoolingInterval")]
		public int NextPoolingInterval { get; set; }

		/// <summary>
		/// 電文リストアイテム
		/// </summary>
		public class Item
		{
			/// <summary>
			/// 電文受信通番
			/// </summary>
			[JsonPropertyName("serial")]
			public int Serial { get; set; }
			/// <summary>
			/// 配信区分により変化。取りうる値は telegram.earthquake, telegram.volcano, telegram.weather, telegram.scheduled
			/// </summary>
			[JsonPropertyName("classification")]
			public string Classification { get; set; }
			/// <summary>
			/// 配信データを区別するユニーク384bitハッシュ
			/// </summary>
			[JsonPropertyName("id")]
			public string Id { get; set; }
			/// <summary>
			/// ヘッダ情報
			/// </summary>
			[JsonPropertyName("head")]
			public Head Head { get; set; }
			/// <summary>
			/// 受信時刻
			/// </summary>
			[JsonPropertyName("receivedTime")]
			public DateTime ReceivedTime { get; set; }
			/// <summary>
			/// XML電文Control,Head情報
			/// </summary>
			[JsonPropertyName("xmlReport")]
			public TelegramXmldata? XmlReport { get; set; }
			/// <summary>
			/// bodyプロパティの表現形式を示す。"xml"、"a/n"、"binary"は気象庁が定めたフォーマット、"json"は本サービスが独自に定めたフォーマット
			/// </summary>
			[JsonPropertyName("format")]
			public string? Format { get; set; }
			/// <summary>
			/// 電文本文URL
			/// </summary>
			[JsonPropertyName("url")]
			public string Url { get; set; }
		}

		/// <summary>
		/// ヘッダ情報
		/// </summary>
		public class Head
		{
			/// <summary>
			/// データ種類コード
			/// </summary>
			[JsonPropertyName("type")]
			public string Type { get; set; }
			/// <summary>
			/// 発表英字官署名
			/// </summary>
			[JsonPropertyName("author")]
			public string Author { get; set; }
			/// <summary>
			/// 基点時刻（ISO8601拡張形式）
			/// </summary>
			[JsonPropertyName("time")]
			public DateTime Time { get; set; }
			/// <summary>
			/// 指示コード
			/// </summary>
			[JsonPropertyName("designation")]
			public string? Designation { get; set; }
			/// <summary>
			/// 訓練、試験等のテスト等電文かどうかを示す
			/// <para>注意：XML電文以外のテスト配信は常にfalseになります。</para>
			/// </summary>
			[JsonPropertyName("test")]
			public bool Test { get; set; }
		}
	}

	[JsonSerializable(typeof(TelegramListResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class TelegramListResponseSerializerContext : JsonSerializerContext
	{
	}
}
