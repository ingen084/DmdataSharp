using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.Parameters
{
	/// <summary>
	/// 津波観測点一覧APIのレスポンス
	/// </summary>
	public class TsunamiStationParameterResponse : DmdataParameterResponse
	{
		/// <summary>
		/// 津波観測点
		/// </summary>
		[JsonPropertyName("items")]
		public Item[]? Items { get; set; }
		/// <summary>
		/// 津波観測点を表す
		/// </summary>
		public class Item
		{
			/// <summary>
			/// 津波予報区名
			/// </summary>
			[JsonPropertyName("area")]
			public string? Area { get; set; }
			/// <summary>
			/// 所在する都道府県
			/// </summary>
			[JsonPropertyName("prefectures")]
			public string? Prefectures { get; set; }
			/// <summary>
			/// 観測点コード（XML）
			/// </summary>
			[JsonPropertyName("code")]
			public string? Code { get; set; }
			/// <summary>
			/// 観測点名
			/// </summary>
			[JsonPropertyName("name")]
			public string? Name { get; set; }
			/// <summary>
			/// 観測点名（カナ）
			/// </summary>
			[JsonPropertyName("kana")]
			public string? Kana { get; set; }
			/// <summary>
			/// 所属する機関
			/// </summary>
			[JsonPropertyName("owner")]
			public string? Owner { get; set; }
			/// <summary>
			/// 所在する緯度
			/// </summary>
			[JsonPropertyName("latitude")]
			public string? Latitude { get; set; }
			/// <summary>
			/// 所在する経度
			/// </summary>
			[JsonPropertyName("longitude")]
			public string? Longitude { get; set; }
		}
	}
}
