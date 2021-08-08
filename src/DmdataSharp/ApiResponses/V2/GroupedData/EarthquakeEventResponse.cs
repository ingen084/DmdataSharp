using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2.GroupedData
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// GD Earthquake Event APIのレスポンス
	/// </summary>
	public class EarthquakeEventResponse : DmdataResponse
	{
		/// <summary>
		/// 地震情報
		/// </summary>
		[JsonPropertyName("event")]
		public EventData Event { get; set; }
		/// <summary>
		/// 地震情報
		/// </summary>
		public class EventData
		{
			/// <summary>
			/// ID
			/// </summary>
			[JsonPropertyName("id")]
			public int Id { get; set; }
			/// <summary>
			/// 地震情報のEventID
			/// </summary>
			[JsonPropertyName("eventId")]
			public string EventId { get; set; }
			/// <summary>
			/// 地震発生時刻
			/// 震度速報のみの場合はnull
			/// </summary>
			[JsonPropertyName("originTime")]
			public DateTime? OriginTime { get; set; }
			/// <summary>
			/// 地震検知時刻
			/// </summary>
			[JsonPropertyName("arrivalTime")]
			public DateTime ArrivalTime { get; set; }
			/// <summary>
			/// 震源要素
			/// 震度速報のみの場合はnull
			/// </summary>
			[JsonPropertyName("hypocenter")]
			public EarthquakeListResponse.Hypocenter? Hypocenter { get; set; }
			/// <summary>
			/// マグニチュード要素
			/// 震度速報のみの場合はnull
			/// </summary>
			[JsonPropertyName("magnitude")]
			public EarthquakeListResponse.Magnitude? Magnitude { get; set; }
			/// <summary>
			/// 最大震度
			/// 観測した震度がない場合はnull
			/// </summary>
			[JsonPropertyName("maxInt")]
			public string? MaxInt { get; set; }
			/// <summary>
			/// 地震情報の電文リスト
			/// </summary>
			[JsonPropertyName("telegrams")]
			public Telegram[] Telegrams { get; set; }
		}

		/// <summary>
		/// 地震情報の電文
		/// </summary>
		public class Telegram
		{
			/// <summary>
			/// 電文受信通番
			/// </summary>
			[JsonPropertyName("serial")]
			public int Serial { get; set; }
			/// <summary>
			/// 配信データを区別するユニーク384bitハッシュ
			/// </summary>
			[JsonPropertyName("id")]
			public string Id { get; set; }
			/// <summary>
			/// 配信区分
			/// 常に telegram.earthquake のはず
			/// </summary>
			[JsonPropertyName("classification")]
			public string Classification { get; set; }
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
			public TelegramXmldata XmlReport { get; set; }
			/// <summary>
			/// 加工データのスキーマ情報
			/// </summary>
			[JsonPropertyName("schema")]
			public Schema Schema { get; set; }
			/// <summary>
			/// bodyプロパティの表現形式
			/// "xml"、"a/n"、"binary"は気象庁が定めたフォーマット、"json"はdmdataが独自に定めたフォーマット
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
			/// 基点時刻
			/// </summary>
			[JsonPropertyName("time")]
			public DateTime Time { get; set; }
			/// <summary>
			/// 指示コード
			/// </summary>
			[JsonPropertyName("designation")]
			public string? Designation { get; set; }
			/// <summary>
			/// 訓練、試験等のテスト等電文かどうか
			/// 常にfalse
			/// </summary>
			[JsonPropertyName("test")]
			public bool Test { get; set; }
		}
		/// <summary>
		/// 加工データのスキーマ情報
		/// </summary>
		public class Schema
		{
			/// <summary>
			/// スキーマ名
			/// </summary>
			[JsonPropertyName("type")]
			public string Type { get; set; }
			/// <summary>
			/// スキーマのバージョン
			/// </summary>
			[JsonPropertyName("version")]
			public string Version { get; set; }
		}
	}
}
