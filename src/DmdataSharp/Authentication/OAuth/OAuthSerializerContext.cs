using System.Text.Json.Serialization;

namespace DmdataSharp.Authentication.OAuth
{
	[JsonSerializable(typeof(JsonWebToken.JwtClaim))]
	[JsonSerializable(typeof(JsonWebToken.JwtHeader))]
	[JsonSerializable(typeof(OAuthErrorResponse))]
	[JsonSerializable(typeof(OAuthTokenResponse))]
	[JsonSerializable(typeof(JsonWebKey))]
	[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
	internal partial class OAuthSerializerContext : JsonSerializerContext
	{
	}
}
