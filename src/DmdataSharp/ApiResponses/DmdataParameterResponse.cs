using System;
using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
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
		public string Version { get; set; }
	}
}
