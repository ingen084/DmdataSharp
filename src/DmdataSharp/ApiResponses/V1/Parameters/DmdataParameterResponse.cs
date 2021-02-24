using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V1.Parameters
{
	/// <summary>
	/// dmdataのParameter APIレスポンスの共通部分を表す
	/// </summary>
	public abstract class DmdataParameterResponse : DmdataResponse
	{
		/// <summary>
		/// 情報の更新日時
		/// </summary>
		[JsonPropertyName("changeTime")]
		public DateTime ChangeTime { get; set; }
		/// <summary>
		/// パラメータのバージョン
		/// </summary>
		[JsonPropertyName("version")]
		public string? Version { get; set; }
	}
}
