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
		/// リクエストに認証情報を付与し、リクエストを実行します
		/// </summary>
		/// <param name="request">付与するHttpRequestMessage</param>
		/// <param name="sendAsync">リクエストを送信するFunc</param>
		/// <returns>レスポンス</returns>
		public override Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request, Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
			=> Credential.ProcessRequestAsync(request, sendAsync);

		/// <summary>
		/// トークンを開放します
		/// </summary>
		public override void Dispose()
		{
			Credential.RevokeAccessTokenAsync();
			base.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
