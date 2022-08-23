using DmdataSharp.ApiResponses;
using DmdataSharp.Authentication;
using DmdataSharp.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace DmdataSharp
{
	/// <summary>
	/// dmdataのAPIクライアントベースクラス
	/// </summary>
	public abstract class DmdataApi : IDisposable
	{
		/// <summary>
		/// HttpClient
		/// </summary>
		protected HttpClient HttpClient { get; }

		/// <summary>
		/// リクエストのタイムアウト時間
		/// </summary>
		public TimeSpan Timeout
		{
			get => HttpClient.Timeout;
			set => HttpClient.Timeout = value;
		}

		/// <summary>
		/// dmdataのAPI認証方法
		/// </summary>
		public Authenticator Authenticator { get; set; }

		/// <summary>
		/// 並列リクエストを許可するか
		/// <para>サーバー負荷･レートリミットの原因となるため推奨しません。</para>
		/// </summary>
		public bool AllowPararellRequest { get; set; } = false;

		/// <summary>
		/// リクエストの並列化を阻止するためのMRE
		/// </summary>
		protected ManualResetEventSlim RequestMre { get; } = new(true);

		/// <summary>
		/// 指定したHttpClient･認証方法で初期化する
		/// </summary>
		/// <param name="httpClient"></param>
		/// <param name="authenticator"></param>
		protected DmdataApi(HttpClient httpClient, Authenticator authenticator)
		{
			HttpClient = httpClient;
			Authenticator = authenticator;
		}

		/// <summary>
		/// APIキーによる初期化互換用コンストラクタ
		/// </summary>
		/// <param name="apiKey">APIキー</param>
		/// <param name="userAgent">UserAgent</param>
		/// <param name="timeout">リクエストタイムアウト時間</param>
		protected DmdataApi(string apiKey, string userAgent, TimeSpan? timeout = null)
		{
			HttpClient = new HttpClient() { Timeout = timeout ?? TimeSpan.FromMilliseconds(5000) };
			HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent ?? throw new ArgumentNullException(nameof(userAgent)));
			Debug.WriteLine("[Dmdata] User-Agent: " + userAgent);
			Authenticator = new ApiKeyAuthenticator(apiKey ?? throw new ArgumentNullException(apiKey));
		}

		/// <summary>
		/// GETリクエストを送信し、Jsonをデシリアライズした結果を取得します。
		/// </summary>
		/// <typeparam name="T">デシリアライズする型</typeparam>
		/// <param name="url">使用するURL</param>
		/// <param name="jsonTypeInfo">レスポンスをデシリアライズするJsonTypeInfo</param>
		/// <returns></returns>
		protected async Task<T> GetJsonObject<T>(string url, JsonTypeInfo<T> jsonTypeInfo) where T : DmdataResponse
		{
			var apl = AllowPararellRequest;
			if (!apl)
			{
				if (!RequestMre.IsSet && !await Task.Run(() => RequestMre.Wait(Timeout)))
					throw new DmdataApiTimeoutException(url);
				RequestMre.Reset();
			}

			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, url);

				using var response = await Authenticator.ProcessRequestAsync(request, r => HttpClient.SendAsync(r));
				switch (response.StatusCode)
				{
					case System.Net.HttpStatusCode.Forbidden:
						throw new DmdataForbiddenException("権限がないもしくは不正な認証情報です。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.PaymentRequired:
						throw new DmdataNotValidContractException("有効な契約が存在しません。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.Unauthorized:
						throw new DmdataUnauthorizedException("認証情報が不正です。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode s when ((int)s / 100) == 5:
						throw new DmdataException("サーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				}

				if (JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync(), jsonTypeInfo) is not T r)
					throw new DmdataException("APIレスポンスをパースできませんでした");
				if (r.Status != "ok")
					throw new DmdataApiErrorException(r);
				return r;
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException(Authenticator.FilterErrorMessage(url)); // ApiKeyは秘匿情報のため出力を行なわない
			}
			finally
			{
				if (!apl)
					RequestMre.Set();
			}
		}

		/// <summary>
		/// POSTリクエストを送信し、Jsonをデシリアライズした結果を取得します。
		/// </summary>
		/// <typeparam name="TRequest">リクエストをシリアライズする型</typeparam>
		/// <typeparam name="TResponse">レスポンスをデシリアライズする型</typeparam>
		/// <param name="url">使用するURL</param>
		/// <param name="body">POSTするbody</param>
		/// <param name="requestJsonTypeInfo">リクエスト型のJsonTypeInfo</param>
		/// <param name="responseJsonTypeInfo">レスポンス型のJsonTypeInfo</param>
		/// <returns></returns>
		protected async Task<TResponse> PostJsonObject<TRequest, TResponse>(string url, TRequest body, JsonTypeInfo<TRequest> requestJsonTypeInfo, JsonTypeInfo<TResponse> responseJsonTypeInfo) where TResponse : DmdataResponse
		{
			var apl = AllowPararellRequest;
			if (!apl)
			{
				if (!RequestMre.IsSet && !await Task.Run(() => RequestMre.Wait(Timeout)))
					throw new DmdataApiTimeoutException(url);
				RequestMre.Reset();
			}

			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Content = new StringContent(JsonSerializer.Serialize(body, requestJsonTypeInfo), Encoding.UTF8, "application/json");

				using var response = await Authenticator.ProcessRequestAsync(request, r => HttpClient.SendAsync(r));
				switch (response.StatusCode)
				{
					case System.Net.HttpStatusCode.Forbidden:
						throw new DmdataForbiddenException("権限がないもしくは不正な認証情報です。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.PaymentRequired:
						throw new DmdataNotValidContractException("有効な契約が存在しません。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.Unauthorized:
						throw new DmdataUnauthorizedException("認証情報が不正です。 URL: " + Authenticator.FilterErrorMessage(url));
#if !NET5_0
					case (System.Net.HttpStatusCode)429:
#else
					case System.Net.HttpStatusCode.TooManyRequests:
#endif
						throw new DmdataRateLimitExceededException(response.Headers.TryGetValues("Retry-After", out var retry) ? retry.FirstOrDefault() : null);
					case System.Net.HttpStatusCode s when ((int)s / 100) == 5:
						throw new DmdataException("サーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				}

				if (JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync(), responseJsonTypeInfo) is not TResponse r)
					throw new DmdataException("APIレスポンスをパースできませんでした");
				if (r.Status != "ok")
					throw new DmdataApiErrorException(r);
				return r;
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException(Authenticator.FilterErrorMessage(url)); // ApiKeyは秘匿情報のため出力を行なわない
			}
			finally
			{
				if (!apl)
					RequestMre.Set();
			}
		}

		/// <summary>
		/// DELETEリクエストを送信し、Jsonをデシリアライズした結果を取得します。レスポンスが空の場合デフォルト値を返します。
		/// </summary>
		/// <typeparam name="T?">デシリアライズする型</typeparam>
		/// <param name="url">使用するURL</param>
		/// <param name="jsonTypeInfo">レスポンスをデシリアライズするJsonTypeInfo</param>
		/// <returns></returns>
		protected async Task<T?> DeleteJsonObject<T>(string url, JsonTypeInfo<T> jsonTypeInfo) where T : DmdataResponse
		{
			var apl = AllowPararellRequest;
			if (!apl)
			{
				if (!RequestMre.IsSet && !await Task.Run(() => RequestMre.Wait(Timeout)))
					throw new DmdataApiTimeoutException(url);
				RequestMre.Reset();
			}

			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Delete, url);

				using var response = await Authenticator.ProcessRequestAsync(request, r => HttpClient.SendAsync(r));
				switch (response.StatusCode)
				{
					case System.Net.HttpStatusCode.Forbidden:
						throw new DmdataForbiddenException("権限がないもしくは不正な認証情報です。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.PaymentRequired:
						throw new DmdataNotValidContractException("有効な契約が存在しません。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.Unauthorized:
						throw new DmdataUnauthorizedException("認証情報が不正です。 URL: " + Authenticator.FilterErrorMessage(url));
#if !NET5_0 && !NET6_0
					case (System.Net.HttpStatusCode)429:
#else
					case System.Net.HttpStatusCode.TooManyRequests:
#endif
						throw new DmdataRateLimitExceededException(response.Headers.TryGetValues("Retry-After", out var retry) ? retry.FirstOrDefault() : null);
					case System.Net.HttpStatusCode s when ((int)s / 100) == 5:
						throw new DmdataException("サーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				}

				var respString = await response.Content.ReadAsStringAsync();
				if (string.IsNullOrWhiteSpace(respString))
					return default;
				if (JsonSerializer.Deserialize(respString, jsonTypeInfo) is not T r)
					throw new DmdataException("APIレスポンスをパースできませんでした");
				if (r.Status != "ok")
					throw new DmdataApiErrorException(r);
				return r;
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException(Authenticator.FilterErrorMessage(url)); // ApiKeyは秘匿情報のため出力を行なわない
			}
			finally
			{
				if (!apl)
					RequestMre.Set();
			}
		}

		/// <summary>
		/// 内部のHttpClientを開放する
		/// </summary>
		public void Dispose()
		{
			Authenticator?.Dispose();
			HttpClient?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
