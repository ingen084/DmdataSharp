using System.Text.Json.Serialization;

namespace DmdataSharp.Authentication.OAuth.JsonWebToken
{
	internal class JwtHeader
	{
		public JwtHeader(string type, string algorithm, JsonWebKey jwk)
		{
			Type = type;
			Algorithm = algorithm;
			Jwk = jwk;
		}

		[JsonPropertyName("typ")]
		public string Type { get; set; }

		[JsonPropertyName("jwk")]
		public JsonWebKey Jwk { get; set; }

		[JsonPropertyName("alg")]
		public string Algorithm { get; set; }
	}
}
