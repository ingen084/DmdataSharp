using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2.GroupedData
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// GD Eew List APIのレスポンス
	/// </summary>
	public class EewListResponse : DmdataResponse
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
		/// EEWアイテム
		/// </summary>
		public class Item
		{
			/// <summary>
			/// ID
			/// </summary>
			[JsonPropertyName("id")]
			public int Id { get; set; }
			/// <summary>
			/// 緊急地震速報のEventID
			/// </summary>
			[JsonPropertyName("eventId")]
			public string EventId { get; set; }
			/// <summary>
			/// 緊急地震速報のEventIDに対する報数
			/// </summary>
			[JsonPropertyName("serial")]
			public int Serial { get; set; }
			/// <summary>
			/// この緊急地震速報（最終報）を発表した時刻
			/// </summary>
			[JsonPropertyName("dateTime")]
			public DateTime DateTime { get; set; }
			/// <summary>
			/// このEventIDに対してこの内容が最終であるかどうかを示し、このAPIでは常に true とする
			/// </summary>
			[JsonPropertyName("isLastInfo")]
			public bool IsLastInfo { get; set; }
			/// <summary>
			/// このEventIDに対して緊急地震速報を取り消されたかどうか
			/// </summary>
			[JsonPropertyName("isCanceled")]
			public bool IsCanceled { get; set; }
			/// <summary>
			/// このEventIDに対して、緊急地震速報の警報を発表されたかどうかを示す
			/// 取消時はnull
			/// </summary>
			[JsonPropertyName("isWarning")]
			public bool? IsWarning { get; set; }
			/// <summary>
			/// 予測震源要素
			/// 取消時はnull
			/// </summary>
			[JsonPropertyName("earthquake")]
			public Earthquake? Earthquake { get; set; }
			/// <summary>
			/// 予測震度要素
			/// 取消時はnull
			/// </summary>
			[JsonPropertyName("intensity")]
			public Intensity? Intensity { get; set; }
			/// <summary>
			/// フリーテキスト
			/// 出現しない場合はnull
			/// </summary>
			[JsonPropertyName("text")]
			public string? Text { get; set; }
		}

		/// <summary>
		/// 予測震源要素
		/// </summary>
		public class Earthquake
		{
			/// <summary>
			/// 地震発生時刻（日本時間）
			/// 発生時刻がない場合はnull（仮定震源要素時や100gal検知報など）
			/// </summary>
			[JsonPropertyName("originTime")]
			public DateTime? OriginTime { get; set; }
			/// <summary>
			/// 地震検知時刻（日本時間）
			/// </summary>
			[JsonPropertyName("arrivalTime")]
			public DateTime ArrivalTime { get; set; }
			/// <summary>
			/// 仮定震源要素
			/// 仮定震源要素時以外の場合はnull
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
			/// <summary>
			/// 地震の震源要素
			/// </summary>
			[JsonPropertyName("hypocenter")]
			public Hypocenter Hypocenter { get; set; }
			/// <summary>
			/// 地震の規模
			/// </summary>
			[JsonPropertyName("magnitude")]
			public Magnitude Magnitude { get; set; }
		}

		/// <summary>
		/// 地震の震源要素
		/// </summary>
		public class Hypocenter
		{
			/// <summary>
			/// 震央地名コード
			/// </summary>
			[JsonPropertyName("code")]
			public string Code { get; set; }
			/// <summary>
			/// 震央地名
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
			/// <summary>
			/// 震源地の空間座標
			/// </summary>
			[JsonPropertyName("coordinate")]
			public EarthquakeListResponse.Coordinate Coordinate { get; set; }
			/// <summary>
			/// 深さ情報
			/// </summary>
			[JsonPropertyName("depth")]
			public Depth Depth { get; set; }
			/// <summary>
			/// 短縮用震央地名
			/// </summary>
			[JsonPropertyName("reduce")]
			public Reduce Reduce { get; set; }
			/// <summary>
			/// 震源の場所が陸域か海域化を判別
			/// 仮定震源要素時や100gal検知報などの場合はnull
			/// <para>取りうる値は <c>内陸</c> <c>海域</c></para>
			/// </summary>
			[JsonPropertyName("landOrSea")]
			public string LandOrSea { get; set; }
			/// <summary>
			/// 震源及びマグニチュードの計算精度情報
			/// </summary>
			[JsonPropertyName("accuracy")]
			public Accuracy Accuracy { get; set; }
		}

		/// <summary>
		/// 深さ情報
		/// </summary>
		public class Depth
		{
			/// <summary>
			/// 深さ情報のタイプ
			/// <c>深さ</c> で固定
			/// </summary>
			[JsonPropertyName("type")]
			public string Type { get; set; }
			/// <summary>
			/// 深さ情報の単位
			/// <c>km</c> で固定
			/// </summary>
			[JsonPropertyName("unit")]
			public string Unit { get; set; }
			/// <summary>
			/// 震源の深さ
			/// 不明の場合はnull
			/// </summary>
			[JsonPropertyName("value")]
			public string? Value { get; set; }
			/// <summary>
			/// 深さの例外的表現
			/// Value の値が0または700またはnull以外の場合はnull
			/// <para>取りうる値は <c>ごく浅い</c> <c>７００ｋｍ以上</c> <c>不明</c></para>
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
		}

		/// <summary>
		/// 短縮用震央地名
		/// </summary>
		public class Reduce
		{
			/// <summary>
			/// 短縮用震央地名コード
			/// </summary>
			[JsonPropertyName("code")]
			public string Code { get; set; }
			/// <summary>
			/// 短縮用震央地名
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
		}

		/// <summary>
		/// 震源及びマグニチュードの計算精度情報
		/// </summary>
		public class Accuracy
		{
			/// <summary>
			/// 震央位置の精度値
			/// <para>[0] は震央位置</para>
			/// <para>[1] は震源位置</para>
			/// </summary>
			[JsonPropertyName("epicenters")]
			public string[] Epicenters { get; set; }
			/// <summary>
			/// 深さの精度値
			/// </summary>
			[JsonPropertyName("depth")]
			public string Depth { get; set; }
			/// <summary>
			/// マグニチュードの精度値
			/// </summary>
			[JsonPropertyName("magnitudeCalculation")]
			public string MagnitudeCalculation { get; set; }
			/// <summary>
			/// マグニチュード計算使用観測点数
			/// </summary>
			[JsonPropertyName("numberOfMagnitudeCalculation")]
			public string NumberOfMagnitudeCalculation { get; set; }
		}

		/// <summary>
		/// 地震の規模
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
			/// 不明時はnull
			/// </summary>
			[JsonPropertyName("value")]
			public string Value { get; set; }
			/// <summary>
			/// マグニチュードの数値が求まらない事項を記載
			/// Value がnull以外の場合はnull
			/// <para>不明時は <c>M不明</c></para>
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
		}

		/// <summary>
		/// 予測震度要素
		/// </summary>
		public class Intensity
		{
			/// <summary>
			/// 最大予測震度
			/// </summary>
			[JsonPropertyName("forecastMaxInt")]
			public ForecastMaxInt ForecastMaxInt { get; set; }
			/// <summary>
			/// 最大予測長周期地震動階級
			/// <para>VXSE43・VXSE45で震源の深さが150km未満の場合でなければnull</para>
			/// </summary>
			[JsonPropertyName("forecastMaxLpgmInt")]
			public ForecastMaxLpgmInt? ForecastMaxLpgmInt { get; set; }
			/// <summary>
			/// 予測震度・予測長周期地震動階級付加要素
			/// <para>度予測及び長周期地震動階級予測をどちらも行っていないために、直前の緊急地震速報と今回の緊急地震速報の間で最大予測震度及び最大予測長周期地震動階級の比較ができない場合、本要素は出現しない</para>
			/// </summary>
			[JsonPropertyName("appendix")]
			public Appendix? Appendix { get; set; }
			/// <summary>
			/// 細分化地域内における予想
			/// 情報により出現
			/// </summary>
			[JsonPropertyName("regions")]
			public Regions? Regions { get; set; }
		}

		/// <summary>
		/// 最大予測震度
		/// </summary>
		public class ForecastMaxInt
		{
			/// <summary>
			/// 最大予測震度の下限
			/// </summary>
			[JsonPropertyName("from")]
			public string From { get; set; }
			/// <summary>
			/// 最大予測震度の上限
			/// </summary>
			[JsonPropertyName("to")]
			public string To { get; set; }
		}

		/// <summary>
		/// 最大予測長周期地震動階級
		/// </summary>
		public class ForecastMaxLpgmInt
		{
			/// <summary>
			/// 最大予測長周期地震動階級の下限
			/// </summary>
			[JsonPropertyName("from")]
			public string From { get; set; }
			/// <summary>
			/// 最大予測長周期地震動階級の上限
			/// </summary>
			[JsonPropertyName("to")]
			public string To { get; set; }
		}

		/// <summary>
		/// 予測震度・予測長周期地震動階級付加要素
		/// </summary>
		public class Appendix
		{
			/// <summary>
			/// 最大予測震度変化
			/// </summary>
			[JsonPropertyName("maxIntChange")]
			public string MaxIntChange { get; set; }
			/// <summary>
			/// 最大予測長周期地震動階級変化
			/// VXSE43・VXSE45以外はnull
			/// </summary>
			[JsonPropertyName("maxLpgmIntChange")]
			public string? MaxLpgmIntChange { get; set; }
			/// <summary>
			/// 最大予測値変化の理由
			/// </summary>
			[JsonPropertyName("maxIntChangeReason")]
			public string MaxIntChangeReason { get; set; }
		}

		/// <summary>
		/// 細分化地域内における予想
		/// </summary>
		public class Regions
		{
			/// <summary>
			/// 細分化地域コード
			/// </summary>
			[JsonPropertyName("code")]
			public string Code { get; set; }
			/// <summary>
			/// 細分化地域名
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
			/// <summary>
			/// PLUM法による震度予測であるか示す
			/// </summary>
			[JsonPropertyName("isPlum")]
			public bool IsPlum { get; set; }
			/// <summary>
			/// 警報発表しているかどうかを示す
			/// </summary>
			[JsonPropertyName("isWarning")]
			public bool IsWarning { get; set; }
			/// <summary>
			/// 最大予測震度
			/// </summary>
			[JsonPropertyName("forecastMaxInt")]
			public ForecastMaxInt ForecastMaxInt { get; set; }
			/// <summary>
			/// 最大予測長周期地震動階級
			/// <para>VXSE43・VXSE45で震源の深さが150km未満の場合でなければnull</para>
			/// </summary>
			[JsonPropertyName("forecastMaxLpgmInt")]
			public ForecastMaxLpgmInt? ForecastMaxLpgmInt { get; set; }
			/// <summary>
			/// 緊急地震速報の種別
			/// </summary>
			[JsonPropertyName("kind")]
			public Kind Kind { get; set; }
			/// <summary>
			/// 主要動到達に関する状況等
			/// <para>主要動の到達予測時刻を過ぎており、既に主要動が到達していると推測される時には出現する</para>
			/// <para><c>既に主要動到達と推測</c> で固定</para>
			/// </summary>
			[JsonPropertyName("condition")]
			public string? Condition { get; set; }
			/// <summary>
			/// 主要動の到達予測時刻
			/// <para>主要動の到達予測時刻以前であり、主要動が未到達と推測される時には、本要素が出現する</para>
			/// <para>該当区域について PLUM法で予測している時には、「PLUM法でその震度（階級震度）を初めて予測した時刻」を示す</para>
			/// </summary>
			[JsonPropertyName("arrivalTime")]
			public DateTime? ArrivalTime { get; set; }
		}

		/// <summary>
		/// 緊急地震速報の種別
		/// </summary>
		public class Kind
		{
			/// <summary>
			/// 緊急地震速報の種別コード
			/// </summary>
			[JsonPropertyName("code")]
			public string Code { get; set; }
			/// <summary>
			/// 緊急地震速報の種別
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
		}
	}

	[JsonSerializable(typeof(EewListResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class EewListResponseSerializerContext : JsonSerializerContext
	{
	}
}
