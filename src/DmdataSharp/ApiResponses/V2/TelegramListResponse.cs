using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// 電文リストの結果
	/// </summary>
	public class TelegramListResponse
	{
		/// <summary>
		/// 電文情報リスト
		/// </summary>
		[JsonPropertyName("items")]
		public TelegramItem[] Items { get; set; }
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
	}

	/// <summary>
	/// 電文リストアイテム
	/// </summary>
	public class TelegramItem
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
		public TelegramHead Head { get; set; }
		/// <summary>
		/// XML電文Control,Head情報
		/// </summary>
		[JsonPropertyName("xmlReport")]
		public TelegramXmldata XmlReport { get; set; }
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
	public class TelegramHead
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

	/// <summary>
	/// XML電文のControl/Head情報
	/// </summary>
	public class TelegramXmldata
	{
		/// <summary>
		/// XML電文のControl
		/// </summary>
		[JsonPropertyName("control")]
		public TelegramXmlControl? Control { get; set; }
		/// <summary>
		/// XML電文のHead情報
		/// </summary>
		[JsonPropertyName("head")]
		public TelegramXmlHead? Head { get; set; }
	}

	/// <summary>
	/// XML電文のControl情報
	/// </summary>
	public class TelegramXmlControl
	{
		/// <summary>
		/// 情報名称
		/// </summary>
		[JsonPropertyName("title")]
		public string? Title { get; set; }
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
		public string? Status { get; set; }
		/// <summary>
		/// 編集官署名
		/// </summary>
		[JsonPropertyName("editorialOffice")]
		public string? EditorialOffice { get; set; }
		/// <summary>
		/// 発表官署名
		/// </summary>
		[JsonPropertyName("publishingOffice")]
		public string? PublishingOffice { get; set; }
	}

	/// <summary>
	/// XML電文のHead情報
	/// </summary>
	public class TelegramXmlHead
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
