using System.Text.Json.Serialization;

namespace DmdataSharp.Authentication.OAuth.JsonWebToken
{
	internal class JwtClaim
	{
		public JwtClaim(string id, string httpMethod, string httpUrl, long issuedAt, string? accessToken = null, string? nonce = null)
		{
			Id = id;
			HttpMethod = httpMethod;
			HttpUrl = httpUrl;
			IssuedAt = issuedAt;
			AccessToken = accessToken;
			Nonce = nonce;
		}

		[JsonPropertyName("jti")]
		public string Id { get; set; }

		[JsonPropertyName("htm")]
		public string HttpMethod { get; set; }

		[JsonPropertyName("htu")]
		public string HttpUrl { get; set; }

		[JsonPropertyName("iat")]
		public long IssuedAt { get; set; }

		[JsonPropertyName("ath")]
		public string? AccessToken { get; set; }

		[JsonPropertyName("nonce")]
		public string? Nonce { get; set; }
	}
}
