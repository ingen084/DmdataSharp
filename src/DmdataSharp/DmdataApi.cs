using DmdataSharp.Authentication;
using DmdataSharp.Exceptions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
		/// dmdataのAPI認証方法
		/// </summary>
		public Authenticator Authenticator { get; set; }

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
		/// <returns></returns>
		protected async Task<T> GetJsonObject<T>(string url)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, url);

				using var response = await HttpClient.SendAsync(await Authenticator.ProcessRequestMessageAsync(request));
				switch (response.StatusCode)
				{
					case System.Net.HttpStatusCode.Forbidden:
						throw new DmdataForbiddenException("権限がないもしくは不正な認証情報です。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode.Unauthorized:
						throw new DmdataUnauthorizedException("認証情報が不正です。 URL: " + Authenticator.FilterErrorMessage(url));
					case System.Net.HttpStatusCode s when ((int)s / 100) == 5:
						throw new DmdataException("サーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				}
				if (!response.IsSuccessStatusCode)
					throw new DmdataException("ステータスコードが不正です: " + response.StatusCode);

				if (JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync()) is T r)
					return r;
				throw new DmdataException("APIレスポンスをパースできませんでした");
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException("dmdataへのリクエストにタイムアウトしました。 URL: " + Authenticator.FilterErrorMessage(url)); // ApiKeyは秘匿情報のため出力を行なわない
			}
		}

		/// <summary>
		/// POSTリクエストを送信し、Jsonをデシリアライズした結果を取得します。
		/// </summary>
		/// <typeparam name="TRequest">リクエストをデシリアライズする型</typeparam>
		/// <typeparam name="TResponse">レスポンスをデシリアライズする型</typeparam>
		/// <param name="url">使用するURL</param>
		/// <param name="body">POSTするbody</param>
		/// <returns></returns>
		protected async Task<TResponse> PostJsonObject<TRequest, TResponse>(string url, TRequest body)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, url);
				request.Content = new StringContent(JsonSerializer.Serialize(body, typeof(TRequest)), Encoding.UTF8, "application/json");

				using var response = await HttpClient.SendAsync(await Authenticator.ProcessRequestMessageAsync(request));
				switch (response.StatusCode)
				{
					case System.Net.HttpStatusCode.Forbidden:
						throw new DmdataForbiddenException("権限がないもしくは不正な認証情報です。 URL: " + Authenticator.FilterErrorMessage(url));
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
				if (!response.IsSuccessStatusCode)
					throw new DmdataException("ステータスコードが不正です: " + response.StatusCode);

				if (JsonSerializer.Deserialize<TResponse>(await response.Content.ReadAsStringAsync()) is TResponse r)
					return r;
				throw new DmdataException("APIレスポンスをパースできませんでした");
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException("dmdataへのリクエストにタイムアウトしました。 URL: " + Authenticator.FilterErrorMessage(url)); // ApiKeyは秘匿情報のため出力を行なわない
			}
		}

		/// <summary>
		/// DELETEリクエストを送信し、Jsonをデシリアライズした結果を取得します。レスポンスが空の場合デフォルト値を返します。
		/// </summary>
		/// <typeparam name="T?">デシリアライズする型</typeparam>
		/// <param name="url">使用するURL</param>
		/// <returns></returns>
		protected async Task<T?> DeleteJsonObject<T>(string url)
		{
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Delete, url);

				using var response = await HttpClient.SendAsync(await Authenticator.ProcessRequestMessageAsync(request));
				switch (response.StatusCode)
				{
					case System.Net.HttpStatusCode.Forbidden:
						throw new DmdataForbiddenException("権限がないもしくは不正な認証情報です。 URL: " + Authenticator.FilterErrorMessage(url));
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
				if (!response.IsSuccessStatusCode)
					throw new DmdataException("ステータスコードが不正です: " + response.StatusCode);
				var respString = await response.Content.ReadAsStringAsync();
				if (string.IsNullOrWhiteSpace(respString))
					return default;
				if (JsonSerializer.Deserialize<T>(respString) is T r)
					return r;
				throw new DmdataException("APIレスポンスをパースできませんでした");
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException("dmdataへのリクエストにタイムアウトしました。 URL: " + Authenticator.FilterErrorMessage(url)); // ApiKeyは秘匿情報のため出力を行なわない
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
