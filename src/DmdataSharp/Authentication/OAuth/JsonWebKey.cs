using DmdataSharp.Exceptions;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace DmdataSharp.Authentication.OAuth
{
	internal class JsonWebKey
	{
		public JsonWebKey(ECDsa key)
		{
			var param = key.ExportParameters(false);
			if (param.Q.X is null || param.Q.Y is null)
				throw new DmdataAuthenticationException("DPoPに使用する公開鍵のパラメータが取得できません");

			Curve = "P-" + key.KeySize;
			KeyType = "EC";
			X = OAuthRefreshTokenCredential.EncodeBase64Url(param.Q.X);
			Y = OAuthRefreshTokenCredential.EncodeBase64Url(param.Q.Y);
		}

		[JsonPropertyName("crv")]
		public string Curve { get; set; }

		[JsonPropertyName("kty")]
		public string KeyType { get; set; }

		[JsonPropertyName("x")]
		public string X { get; set; }

		[JsonPropertyName("y")]
		public string Y { get; set; }
	}
}
