using System.Text.Json.Serialization;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthエンドポイントのエラーレスポンス
	/// </summary>
	public class OAuthErrorResponse
	{
		/// <summary>
		/// エラーコード
		/// </summary>
		[JsonPropertyName("error")]
		public string? Error { get; set; }
		/// <summary>
		/// エラー詳細
		/// </summary>
		[JsonPropertyName("error_description")]
		public string? ErrorDescription { get; set; }
	}
}
