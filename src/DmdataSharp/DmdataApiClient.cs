﻿using DmdataSharp.ApiResponses;
using DmdataSharp.ApiResponses.Parameters;
using DmdataSharp.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace DmdataSharp
{
	/// <summary>
	/// dmdataのAPIクライアント
	/// </summary>
	public class DmdataApiClient : IDisposable
	{
		private HttpClient HttpClient { get; }
		/// <summary>
		/// 利用中のAPIキー
		/// </summary>
		public string ApiKey { get; set; }
		/// <summary>
		/// 利用中のUserAgent
		/// </summary>
		public string? UserAgent
		{
			get => HttpClient.DefaultRequestHeaders.GetValues("User-Agent")?.FirstOrDefault();
		}

		/// <summary>
		/// dmdataのAPIクライアントを初期化します
		/// </summary>
		/// <param name="apiKey">利用するAPIキー</param>
		/// <param name="overrideUserAgent">使用する User-Agent 自身のソフトの名前にしておくことをおすすめします</param>
		/// <param name="timeout">タイムアウト時間</param>
		public DmdataApiClient(string apiKey, string? overrideUserAgent = null, TimeSpan? timeout = null)
		{
			HttpClient = new HttpClient() { Timeout = timeout ?? TimeSpan.FromMilliseconds(5000) };
			ApiKey = apiKey ?? throw new ArgumentNullException(apiKey);

			var currentAssemblyName = Assembly.GetExecutingAssembly().GetName();
			var userAgent = overrideUserAgent ?? currentAssemblyName.Name + "/" + (currentAssemblyName.Version?.ToString() ?? "DEBUG");
			HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
			Debug.WriteLine("[Dmdata] User-Agent: " + userAgent);
		}

		/// <summary>
		/// 課金情報を取得する
		/// <para>billing.get が必要です</para>
		/// </summary>
		/// <returns>課金情報</returns>
		public Task<BillingResponse> GetBillingInfoAsync()
			=> GetJsonObject<BillingResponse>($"https://api.dmdata.jp/billing/v1/get?key={ApiKey}");

		/// <summary>
		/// WebSocketのURLを取得する
		/// <para>socket.start/取得する情報に合わせた各権限が必要です</para>
		/// </summary>
		/// <param name="get">WebSocketで取得する配信区分 コンマで区切る telegram.earthquakeなど</param>
		/// <param name="memo">管理画面から表示できる識別文字</param>
		/// <returns></returns>
		public async Task<SocketStartResponse> GetSocketStartAsync(string get, string? memo = null)
		{
			var parameterMap = new Dictionary<string, string?>()
			{
				{ "key", ApiKey },
				{ "get", get },
			};
			if (!string.IsNullOrWhiteSpace(memo))
				parameterMap["memo"] = memo;

#pragma warning disable CS8620 // FormUrlEncodedContentが <string?, string?> を要求しているがDictionaryのKeyはNotNullのため警告回避
			return await GetJsonObject<SocketStartResponse>($"https://api.dmdata.jp/socket/v1/start?" + await new FormUrlEncodedContent(parameterMap).ReadAsStringAsync());
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
		}
		/// <summary>
		/// WebSocketのURLを取得する
		/// <para>socket.start/取得する情報に合わせた各権限が必要です</para>
		/// </summary>
		/// <param name="get">WebSocketで取得する配信区分の配列</param>
		/// <param name="memo">管理画面から表示できる識別文字</param>
		/// <returns></returns>
		public Task<SocketStartResponse> GetSocketStartAsync(IEnumerable<TelegramCategory> get, string? memo = null)
			=> GetSocketStartAsync(string.Join(
#if NET472 || NETSTANDARD2_0
				",",
#else
				',',
#endif
				get.Select(g => g.ToParameterString())), memo);

		/// <summary>
		/// 電文リストを取得する
		/// <para>telegram.list が必要です</para>
		/// </summary>
		/// <param name="type">検索する電文ヘッダ 前方一致</param>
		/// <param name="xml">XML電文のControl/Headを表示するか</param>
		/// <param name="showTest">訓練･試験等のテスト等電文を取得するか</param>
		/// <param name="testOnly">訓練･試験等のテスト等電文のみ取得するか</param>
		/// <param name="newCatch">前回のレスポンスの値を入れると前回以降の新しい情報のみを取得</param>
		/// <param name="nextToken">前回のレスポンスの値を入れると前回以前の古い情報のみを取得</param>
		/// <param name="limit">取得する電文数</param>
		/// <returns>電文リスト情報</returns>
		public async Task<TelegramListResponse> GetTelegramListAsync(
			string? type = null,
			bool xml = false,
			bool showTest = false,
			bool testOnly = false,
			int newCatch = 0,
			string? nextToken = null,
			int limit = 100
			)
		{
			var parameterMap = new Dictionary<string, string?>()
			{
				{ "key", ApiKey },
			};
			if (!string.IsNullOrWhiteSpace(type))
				parameterMap["type"] = type;
			if (xml)
				parameterMap["xml"] = "true";
			if (showTest)
				parameterMap["test"] = "true";
			if (testOnly)
				parameterMap["test"] = "only";
			if (newCatch != 0)
				parameterMap["newCatch"] = newCatch.ToString();
			if (!string.IsNullOrWhiteSpace(nextToken))
				parameterMap["nextToken"] = nextToken;
			if (limit != 100)
				parameterMap["limit"] = limit.ToString();

#pragma warning disable CS8620 // FormUrlEncodedContentが <string?, string?> を要求しているがDictionaryのKeyはNotNullのため警告回避
			return await GetJsonObject<TelegramListResponse>($"https://api.dmdata.jp/telegram/v1/list?" + await new FormUrlEncodedContent(parameterMap).ReadAsStringAsync());
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
		}
		/// <summary>
		/// 電文のStreamを取得する
		/// <para>各電文の種類に合わせた権限が必要です</para>
		/// <para>StreamはかならずDisposeしてください！</para>
		/// </summary>
		/// <param name="telegramKey">取得する電文のID</param>
		/// <returns>レスポンスのStream</returns>
		public async Task<Stream> GetTelegramStreamAsync(string telegramKey)
		{
			var url = $"https://data.api.dmdata.jp/v1/{telegramKey}?key={ApiKey}";
			try
			{
				var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead); // サイズのでかいファイルの可能性があるためHeader取得時点で制御を返してもらう
				if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
					throw new DmdataForbiddenException("APIキーに権限がないもしくは不正なAPIキーです。 URL: " + url.Replace(ApiKey, "*API_KEY*"));
				if (((int)response.StatusCode / 100) == 5)
					throw new DmdataException("dmdataでサーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				return await response.Content.ReadAsStreamAsync();
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException("dmdataへのリクエストにタイムアウトしました。 URL: " + url.Replace(ApiKey, "*API_KEY*"));
			}
		}

		/// <summary>
		/// 地震観測地点の情報を取得します
		/// </summary>
		/// <returns>地震観測地点の情報</returns>
		public Task<EarthquakeStationParameterResponse> GetEarthquakeStationParameterAsync()
			=> GetJsonObject<EarthquakeStationParameterResponse>($"https://api.dmdata.jp/parameters/v1/earthquake/station.json?key=" + ApiKey);

		/// <summary>
		/// 津波観測地点の情報を取得します
		/// </summary>
		/// <returns>津波観測地点の情報</returns>
		public Task<TsunamiStationParameterResponse> GetTsunamiStationParameterAsync()
			=> GetJsonObject<TsunamiStationParameterResponse>($"https://api.dmdata.jp/parameters/v1/tsunami/station.json?key=" + ApiKey);

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
				using var response = await HttpClient.GetAsync(url);
				if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
					throw new DmdataForbiddenException("APIキーに権限がないもしくは不正なAPIキーです。 URL: " + url.Replace(ApiKey, "*API_KEY*")); // ApiKeyは秘匿情報のため出力を行なわない
				if (((int)response.StatusCode / 100) == 5)
					throw new DmdataException("dmdataでサーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				if (JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync()) is T r)
					return r;
				throw new DmdataException("APIレスポンスをパースできませんでした");
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException("dmdataへのリクエストにタイムアウトしました。 URL: " + url.Replace(ApiKey, "*API_KEY*")); // ApiKeyは秘匿情報のため出力を行なわない
			}
		}

		/// <summary>
		/// 内部のHttpClientを開放する
		/// </summary>
		public void Dispose()
		{
			HttpClient?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
