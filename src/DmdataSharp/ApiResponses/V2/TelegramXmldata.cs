using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// XML電文のControl/Head情報
	/// </summary>
	public class TelegramXmldata
	{
		/// <summary>
		/// XML電文のControl
		/// </summary>
		[JsonPropertyName("control")]
		public XmlControl Control { get; set; }
		/// <summary>
		/// XML電文のHead情報
		/// </summary>
		[JsonPropertyName("head")]
		public XmlHead Head { get; set; }

		/// <summary>
		/// XML電文のControl情報
		/// </summary>
		public class XmlControl
		{
			/// <summary>
			/// 情報名称
			/// </summary>
			[JsonPropertyName("title")]
			public string Title { get; set; }
			/// <summary>
			/// 発表時刻
			/// </summary>
			[JsonPropertyName("dateTime")]
			public DateTime DateTime { get; set; }
			/// <summary>
			/// 運用種別
			/// <para>"通常" 以外利用してはいけない</para>
			/// <para>通常/試験/訓練</para>
			/// </summary>
			[JsonPropertyName("status")]
			public string Status { get; set; }
			/// <summary>
			/// 編集官署名
			/// </summary>
			[JsonPropertyName("editorialOffice")]
			public string EditorialOffice { get; set; }
			/// <summary>
			/// 発表官署名
			/// </summary>
			[JsonPropertyName("publishingOffice")]
			public string PublishingOffice { get; set; }
		}

		/// <summary>
		/// XML電文のHead情報
		/// </summary>
		public class XmlHead
		{
			/// <summary>
			/// 情報表題
			/// </summary>
			[JsonPropertyName("title")]
			public string? Title { get; set; }
			/// <summary>
			/// 公式な発表時刻
			/// </summary>
			[JsonPropertyName("reportDateTime")]
			public DateTime ReportDateTime { get; set; }
			/// <summary>
			/// 基点時刻
			/// </summary>
			[JsonPropertyName("targetDateTime")]
			public DateTime TargetDateTime { get; set; }
			/// <summary>
			/// 基点時刻のあいまいさ（頃、など）
			/// </summary>
			[JsonPropertyName("targetDateTimeDubious")]
			public string? TargetDateTimeDubious { get; set; }
			/// <summary>
			/// 予報期間
			/// </summary>
			[JsonPropertyName("targetDuration")]
			public string? TargetDuration { get; set; }
			/// <summary>
			/// 情報の失効時刻
			/// </summary>
			[JsonPropertyName("validDateTime")]
			public DateTime? ValidDateTime { get; set; }
			/// <summary>
			/// 電文識別情報
			/// </summary>
			[JsonPropertyName("eventId")]
			public string? EventID { get; set; }
			/// <summary>
			/// 電文情報番号
			/// </summary>
			[JsonPropertyName("serial")]
			public string? Serial { get; set; }
			/// <summary>
			/// 電文発表形態
			/// <para>発表/訂正/遅延/取消</para>
			/// </summary>
			[JsonPropertyName("infoType")]
			public string? InfoType { get; set; }
			/// <summary>
			/// XML電文スキーマの運用種別情報
			/// </summary>
			[JsonPropertyName("infoKind")]
			public string? InfoKind { get; set; }
			/// <summary>
			/// XML電文スキーマの運用種別情報のバージョン
			/// </summary>
			[JsonPropertyName("infoKindVersion")]
			public string? InfoKindVersion { get; set; }
			/// <summary>
			/// 見出し文
			/// </summary>
			[JsonPropertyName("headline")]
			public string? Headline { get; set; }
		}
	}
}
