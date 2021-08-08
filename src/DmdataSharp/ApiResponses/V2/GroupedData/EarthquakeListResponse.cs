using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2.GroupedData
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// GD Earthquake List APIのレスポンス
	/// </summary>
	public class EarthquakeListResponse : DmdataResponse
	{
		/// <summary>
		/// アイテムリスト
		/// </summary>
		[JsonPropertyName("items")]
		public Item[] Items { get; set; }

		/// <summary>
		/// 次のリソースがある場合、取得するためのトークン
		/// </summary>
		[JsonPropertyName("nextToken")]
		public string? NextToken { get; set; }
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
		/// 地震情報アイテム
		/// </summary>
		public class Item
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
			public Hypocenter? Hypocenter { get; set; }
			/// <summary>
			/// マグニチュード要素
			/// 震度速報のみの場合はnull
			/// </summary>
			[JsonPropertyName("magnitude")]
			public Magnitude Magnitude { get; set; }
			/// <summary>
			/// 最大震度
			/// 観測した震度がない場合はnull
			/// </summary>
			[JsonPropertyName("maxInt")]
			public string? MaxInt { get; set; }
		}

		/// <summary>
		/// 震源要素
		/// </summary>
		public class Hypocenter
		{
			/// <summary>
			/// 震央地名
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
			/// <summary>
			/// 震央地名コード
			/// </summary>
			[JsonPropertyName("code")]
			public string Code { get; set; }
			/// <summary>
			/// 震源地の空間座標
			/// </summary>
			[JsonPropertyName("coordinate")]
			public Coordinate Coordinate { get; set; }
			/// <summary>
			/// 深さ情報
			/// </summary>
			[JsonPropertyName("depth")]
			public Depth Depth { get; set; }
			/// <summary>
			/// 震源地の詳細
			/// </summary>
			[JsonPropertyName("detailed")]
			public Detailed? Detailed { get; set; }
			/// <summary>
			/// 震源位置の補足情報
			/// </summary>
			[JsonPropertyName("auxiliary")]
			public Auxiliary? Auxiliary { get; set; }
		}

		/// <summary>
		/// 震源地の空間座標
		/// </summary>
		public class Coordinate
		{
			/// <summary>
			/// 経度
			/// 不明時はnull
			/// </summary>
			[JsonPropertyName("latitude")]
			public CoordinateElement? Latitude { get; set; }
			/// <summary>
			/// 緯度
			/// 不明時はnull
			/// </summary>
			[JsonPropertyName("longitude")]
			public CoordinateElement? Longitude { get; set; }
			/// <summary>
			/// 高さ
			/// 不明･未定義時はnull
			/// </summary>
			[JsonPropertyName("height")]
			public Height Height { get; set; }
			/// <summary>
			/// 測地系に関する情報
			/// "世界測地系" または "日本測地系" が入る
			/// </summary>
			[JsonPropertyName("geodeticSystem")]
			public string? GeodeticSystem { get; set; }
			/// <summary>
			/// "不明" が入る
			/// 不明時以外はnull
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
		}

		/// <summary>
		/// 座標を構成する値
		/// </summary>
		public class CoordinateElement
		{
			/// <summary>
			/// テキスト文
			/// </summary>
			[JsonPropertyName("text")]
			public string Text { get; set; }
			/// <summary>
			/// 数値
			/// </summary>
			[JsonPropertyName("value")]
			public string Value { get; set; }
		}

		/// <summary>
		/// 高さ
		/// </summary>
		public class Height
		{
			/// <summary>
			/// "高さ" で固定
			/// </summary>
			[JsonPropertyName("type")]
			public string Type { get; set; }
			/// <summary>
			/// 高さの単位
			/// "m" で固定
			/// </summary>
			[JsonPropertyName("unit")]
			public string Unit { get; set; }
			/// <summary>
			/// 高さの数値
			/// </summary>
			[JsonPropertyName("value")]
			public string Value { get; set; }
		}

		/// <summary>
		/// 深さ
		/// </summary>
		public class Depth
		{
			/// <summary>
			/// "深さ"で固定
			/// </summary>
			[JsonPropertyName("type")]
			public string Type { get; set; }
			/// <summary>
			/// 深さの単位
			/// "km"で固定
			/// </summary>
			[JsonPropertyName("unit")]
			public string Unit { get; set; }
			/// <summary>
			/// 深さの数値
			/// 不明時はnull
			/// </summary>
			[JsonPropertyName("value")]
			public string? Value { get; set; }
			/// <summary>
			/// 深さの例外的表現
			/// 取りうる値は "ごく浅い" "７００ｋｍ以上" "不明"
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
		}

		/// <summary>
		/// 地震の規模（マグニチュード）
		/// </summary>
		public class Magnitude
		{
			/// <summary>
			/// "マグニチュード" で固定
			/// </summary>
			[JsonPropertyName("type")]
			public string Type { get; set; }
			/// <summary>
			/// マグニチュードの種別
			/// "Mj" または "M" が入る
			/// </summary>
			[JsonPropertyName("unit")]
			public string Unit { get; set; }
			/// <summary>
			/// マグニチュードの数値
			/// 不明時またはM8以上の巨大地震と推測される場合は null
			/// </summary>
			[JsonPropertyName("value")]
			public string? Value { get; set; }
			/// <summary>
			/// マグニチュードの数値が求まらない場合
			/// "不明" 又は "Ｍ８を超える巨大地震" が入る
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
		}

		/// <summary>
		/// 詳細震央地名
		/// </summary>
		public class Detailed
		{
			/// <summary>
			/// 詳細震央地コード
			/// </summary>
			[JsonPropertyName("code")]
			public string Code { get; set; }
			/// <summary>
			/// 詳細震央地名
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
		}
		/// <summary>
		/// 震源位置の補足情報
		/// </summary>
		public class Auxiliary
		{
			/// <summary>
			/// 震源位置の捕捉位置
			/// </summary>
			[JsonPropertyName("text")]
			public string Text { get; set; }
			/// <summary>
			/// 代表地域コード
			/// </summary>
			[JsonPropertyName("code")]
			public int? Code { get; set; }
			/// <summary>
			/// 代表地域名
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
			/// <summary>
			/// 代表地域から震源への方角
			/// </summary>
			[JsonPropertyName("direction")]
			public string Direction { get; set; }
			/// <summary>
			/// 距離情報
			/// </summary>
			[JsonPropertyName("distance")]
			public Distance Distance { get; set; }
		}
		/// <summary>
		/// 距離情報
		/// </summary>
		public class Distance
		{
			/// <summary>
			/// "km"で固定
			/// </summary>
			[JsonPropertyName("unit")]
			public string Unit { get; set; }
			/// <summary>
			/// 代表地域から震源への方角
			/// 16方位
			/// </summary>
			[JsonPropertyName("value")]
			public string Value { get; set; }
		}
	}
}
