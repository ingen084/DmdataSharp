using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace DmdataSharp.Authentication.OAuth
{
	/// <summary>
	/// OAuthによる認証
	/// </summary>
	public class OAuthAuthenticator : Authenticator
	{
		/// <summary>
		/// 認証情報
		/// </summary>
		public OAuthCredential Credential { get; }

		/// <summary>
		/// OAuthによる認証を初期化します
		/// </summary>
		/// <param name="credential"></param>
		public OAuthAuthenticator(OAuthCredential credential)
		{
			Credential = credential ?? throw new ArgumentNullException(nameof(credential));
		}

		/// <summary>
		/// HttpRequestMessageに認証情報を付加します
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async override Task<HttpRequestMessage> ProcessRequestMessageAsync(HttpRequestMessage message)
		{
			message.Headers.TryAddWithoutValidation("Authorization", "Bearer " + await Credential.GetOrUpdateAccessTokenAsync());
			return message;
		}

		/// <summary>
		/// トークンを開放します
		/// </summary>
		public override void Dispose()
		{
			Credential.RevokeAccessTokenAsync();
			base.Dispose();
		}
	}
}
