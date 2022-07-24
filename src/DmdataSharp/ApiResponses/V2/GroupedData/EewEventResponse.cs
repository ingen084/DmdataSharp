using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2.GroupedData
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// GD Eew Event APIのレスポンス
	/// </summary>
	public class EewEventResponse : DmdataResponse
	{
		/// <summary>
		/// アイテムリスト
		/// </summary>
		[JsonPropertyName("items")]
		public Item[] Items { get; set; }

		/// <summary>
		/// EEWアイテム
		/// </summary>
		public class Item
		{
			/// <summary>
			/// 受信ID
			/// </summary>
			[JsonPropertyName("id")]
			public int Id { get; set; }
			/// <summary>
			/// 緊急地震速報のEventID
			/// </summary>
			[JsonPropertyName("eventId")]
			public string EventId { get; set; }
			/// <summary>
			/// EventIDに対するこの情報の情報番号
			/// </summary>
			[JsonPropertyName("serial")]
			public int Serial { get; set; }
			/// <summary>
			/// この情報を発表した時刻
			/// </summary>
			[JsonPropertyName("dateTime")]
			public DateTime DateTime { get; set; }
			/// <summary>
			/// 最終であるかどうか
			/// </summary>
			[JsonPropertyName("isLastInfo")]
			public bool IsLastInfo { get; set; }
			/// <summary>
			/// 緊急地震速報を取り消されたかどうか
			/// </summary>
			[JsonPropertyName("isCanceled")]
			public bool IsCanceled { get; set; }
			/// <summary>
			/// この情報発表時、緊急地震速報の警報を発表されたかどうか
			/// 取消時はnull
			/// </summary>
			[JsonPropertyName("isWarning")]
			public bool? IsWarning { get; set; }
			/// <summary>
			/// 予測震源要素
			/// 取消時はnull
			/// </summary>
			[JsonPropertyName("earthquake")]
			public EewListResponse.Earthquake? Earthquake { get; set; }
			/// <summary>
			/// 予測震度要素
			/// 取消時・震度未計算時はnull
			/// </summary>
			[JsonPropertyName("intensity")]
			public EewListResponse.Intensity? Intensity { get; set; }
			/// <summary>
			/// フリーテキスト
			/// 出現しない場合はnull
			/// </summary>
			[JsonPropertyName("text")]
			public string? Text { get; set; }
			/// <summary>
			/// 緊急地震速報の電文リスト、配列中の要素は1個で固定
			/// </summary>
			[JsonPropertyName("telegrams")]
			public Telegram[] Telegrams { get; set; }
		}

		/// <summary>
		/// 緊急地震速報の電文
		/// </summary>
		public class Telegram
		{
			/// <summary>
			/// 電文受信通番
			/// </summary>
			[JsonPropertyName("serial")]
			public int Serial { get; set; }
			/// <summary>
			/// 配信データ(JSON)を区別するユニーク384bitハッシュ
			/// </summary>
			[JsonPropertyName("id")]
			public string Id { get; set; }
			/// <summary>
			/// 配信データ(XML)を区別するユニーク384bitハッシュ
			/// </summary>
			[JsonPropertyName("originalId")]
			public string OriginalId { get; set; }
			/// <summary>
			/// 配信区分
			/// 取りうる値は <c>eew.forecast</c>
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
			public EarthquakeEventResponse.Schema Schema { get; set; }
			/// <summary>
			/// bodyプロパティの表現形式
			/// "xml"、"a/n"、"binary"は気象庁が定めたフォーマット、"json"はdmdataが独自に定めたフォーマット
			/// </summary>
			[JsonPropertyName("format")]
			public string Format { get; set; }
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
			public object Designation { get; set; }
			/// <summary>
			/// 訓練、試験等のテスト等電文かどうか
			/// 常にfalse
			/// </summary>
			[JsonPropertyName("test")]
			public bool Test { get; set; }
		}
	}

	[JsonSerializable(typeof(EewEventResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class EewEventResponseSerializerContext : JsonSerializerContext
	{
	}
}
