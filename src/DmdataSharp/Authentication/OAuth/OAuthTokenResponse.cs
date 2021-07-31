using System.Text.Json.Serialization;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthトークン取得APIのレスポンス
	/// </summary>
	public class OAuthTokenResponse
	{
		/// <summary>
		/// 取得したアクセストークン
		/// </summary>
		[JsonPropertyName("access_token")]
		public string? AccessToken { get; set; }
		/// <summary>
		/// トークンの種類
		/// </summary>
		[JsonPropertyName("token_type")]
		public string? TokenType { get; set; }
		/// <summary>
		/// トークンの期限(秒)
		/// </summary>
		[JsonPropertyName("expires_in")]
		public int? ExpiresIn { get; set; }
		/// <summary>
		/// 取得したリフレッシュトークン
		/// </summary>
		[JsonPropertyName("refresh_token")]
		public string? RefreshToken { get; set; }
		/// <summary>
		/// 認可されたスコープ
		/// </summary>
		[JsonPropertyName("scope")]
		public string? Scope { get; set; }
	}
}
