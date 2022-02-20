using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V1
{
	/// <summary>
	/// OAuth v1 Introspect APIのレスポンス
	/// </summary>
	public class OAuthIntrospectResponse
	{
		/// <summary>
		/// エラーコード
		/// </summary>
		[JsonPropertyName("error")]
		public string? Error { get; set; }
		/// <summary>
		/// エラーの詳細
		/// </summary>
		[JsonPropertyName("error_description")]
		public string? ErrorDescription { get; set; }

		/// <summary>
		/// トークンが有効であるか
		/// </summary>
		[JsonPropertyName("active")]
		public bool Active { get; set; }
		/// <summary>
		/// アクセストークンに付与されたスコープ。
		/// </summary>
		[JsonPropertyName("scope")]
		public string? Scope { get; set; }
		/// <summary>
		/// OAuth クライアント毎に割り当てられた、CId.で始まるID
		/// </summary>
		[JsonPropertyName("client_id")]
		public string? ClientId { get; set; }
		/// <summary>
		/// OAuthクライアント名
		/// </summary>
		[JsonPropertyName("aud")]
		public string? Aud { get; set; }
		/// <summary>
		/// トークンのユニーク値
		/// </summary>
		[JsonPropertyName("sub")]
		public string? Sub { get; set; }
		/// <summary>
		/// 認可したユーザーのメールアドレス
		/// </summary>
		[JsonPropertyName("username")]
		public string? Username { get; set; }
		/// <summary>
		/// 発行者URL
		/// </summary>
		[JsonPropertyName("iss")]
		public string? Iss { get; set; }
		/// <summary>
		/// トークン発行UNIX時間
		/// </summary>
		[JsonPropertyName("iat")]
		public int? Iat { get; set; }
		/// <summary>
		/// トークン失効UNIX時間
		/// </summary>
		[JsonPropertyName("exp")]
		public int? Exp { get; set; }
	}
}
