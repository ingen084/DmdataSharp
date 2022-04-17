using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
	/// <summary>
	/// 契約中、未契約の情報リストのレスポンス
	/// </summary>
	public class ContractListResponse : DmdataResponse
	{
		/// <summary>
		/// アイテムリスト
		/// </summary>
		[JsonPropertyName("items")]
		public Item[] Items { get; set; }

		/// <summary>
		/// 契約中、未契約の情報リスト
		/// </summary>
		public class Item
		{
			/// <summary>
			/// 契約ID
			/// </summary>
			[JsonPropertyName("id")]
			public int? Id { get; set; }
			/// <summary>
			/// 区分ID
			/// </summary>
			[JsonPropertyName("planId")]
			public int PlanId { get; set; }
			/// <summary>
			/// 区分名
			/// </summary>
			[JsonPropertyName("planName")]
			public string PlanName { get; set; }
			/// <summary>
			/// 区分API名
			/// </summary>
			[JsonPropertyName("classification")]
			public string Classification { get; set; }
			/// <summary>
			/// 価格
			/// </summary>
			[JsonPropertyName("price")]
			public Price Price { get; set; }
			/// <summary>
			/// 契約開始日時
			/// </summary>
			[JsonPropertyName("start")]
			public DateTime? Start { get; set; }
			/// <summary>
			/// 有効かどうか
			/// </summary>
			[JsonPropertyName("isValid")]
			public bool IsValid { get; set; }
			/// <summary>
			/// この契約でWebSocketに接続できる数が増える量
			/// </summary>
			[JsonPropertyName("connectionCounts")]
			public int ConnectionCounts { get; set; }
		}

		/// <summary>
		/// 契約の価格
		/// </summary>
		public class Price
		{
			/// <summary>
			/// 1日当たりの価格（円）
			/// </summary>
			[JsonPropertyName("day")]
			public int Day { get; set; }
			/// <summary>
			/// 月当たり最大の価格（円）
			/// </summary>
			[JsonPropertyName("month")]
			public int Month { get; set; }
		}
	}

	[JsonSerializable(typeof(ContractListResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class ContractListResponseSerializerContext : JsonSerializerContext
	{
	}
}
