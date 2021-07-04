using DmdataSharp.ApiResponses.V1;
using DmdataSharp.ApiResponses.V1.Parameters;
using DmdataSharp.Authentication;
using DmdataSharp.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DmdataSharp
{
	/// <summary>
	/// dmdataのAPIクライアント
	/// </summary>
	public class DmdataV1ApiClient : DmdataApi
	{
		/// <summary>
		/// 利用中のUserAgent
		/// </summary>
		public string? UserAgent
			=> HttpClient.DefaultRequestHeaders.GetValues("User-Agent")?.FirstOrDefault();

		/// <summary>
		/// dmdataのAPI V1クライアントを初期化します
		/// </summary>
		/// <param name="apiKey">利用するAPIキー</param>
		/// <param name="userAgent">使用する User-Agent 自身のソフトの名前にしてください</param>
		/// <param name="timeout">タイムアウト時間</param>
		[Obsolete]
		public DmdataV1ApiClient(string apiKey, string userAgent, TimeSpan? timeout = null) : base(apiKey, userAgent, timeout) { }
		/// <summary>
		/// dmdataのAPI V1クライアントを初期化します
		/// </summary>
		/// <param name="client">内部で使用するHttpClient</param>
		/// <param name="authenticator">使用する認証</param>
		public DmdataV1ApiClient(HttpClient client, Authenticator authenticator) : base(client, authenticator) { }

		/// <summary>
		/// 課金情報を取得する
		/// <para>billing.get が必要です</para>
		/// </summary>
		/// <returns>課金情報</returns>
		[Obsolete]
		public Task<BillingResponse> GetBillingInfoAsync()
			=> GetJsonObject<BillingResponse>("https://api.dmdata.jp/billing/v1/get");

		/// <summary>
		/// WebSocketのURLを取得する
		/// <para>socket.start/取得する情報に合わせた各権限が必要です</para>
		/// </summary>
		/// <param name="get">WebSocketで取得する配信区分 コンマで区切る telegram.earthquakeなど</param>
		/// <param name="memo">管理画面から表示できる識別文字</param>
		/// <returns></returns>
		[Obsolete]
		public async Task<SocketStartResponse> GetSocketStartAsync(string get, string? memo = null)
		{
			var parameterMap = new Dictionary<string, string?>()
			{
				{ "get", get },
			};
			if (!string.IsNullOrWhiteSpace(memo))
				parameterMap["memo"] = memo;
			return await GetJsonObject<SocketStartResponse>($"https://api.dmdata.jp/socket/v1/start?" + await new FormUrlEncodedContent(parameterMap).ReadAsStringAsync());
		}
		/// <summary>
		/// WebSocketのURLを取得する
		/// <para>socket.start/取得する情報に合わせた各権限が必要です</para>
		/// </summary>
		/// <param name="get">WebSocketで取得する配信区分の配列</param>
		/// <param name="memo">管理画面から表示できる識別文字</param>
		/// <returns></returns>
		[Obsolete]
		public Task<SocketStartResponse> GetSocketStartAsync(IEnumerable<TelegramCategoryV1> get, string? memo = null)
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
		/// <para>ポーリングする場合は必ずnewCatchを使用してください！</para>
		/// </summary>
		/// <param name="type">検索する電文ヘッダ 前方一致</param>
		/// <param name="xml">XML電文のControl/Headを表示するか</param>
		/// <param name="showTest">訓練･試験等のテスト等電文を取得するか</param>
		/// <param name="testOnly">訓練･試験等のテスト等電文のみ取得するか</param>
		/// <param name="newCatch">前回のレスポンスの値を入れると前回以降の新しい情報のみを取得できる</param>
		/// <param name="nextToken">前回のレスポンスの値を入れると前回以前の古い情報のみを取得</param>
		/// <param name="limit">取得する電文数</param>
		/// <returns>電文リスト情報</returns>
		[Obsolete]
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
			var parameterMap = new Dictionary<string, string?>();
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
			return await GetJsonObject<TelegramListResponse>($"https://api.dmdata.jp/telegram/v1/list?" + await new FormUrlEncodedContent(parameterMap).ReadAsStringAsync());
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
			var url = $"https://data.api.dmdata.jp/v1/{telegramKey}";
			try
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, url);
				var response = await HttpClient.SendAsync(await Authenticator.ProcessRequestMessageAsync(request), HttpCompletionOption.ResponseHeadersRead); // サイズのでかいファイルの可能性があるためHeader取得時点で制御を返してもらう
				if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
					throw new DmdataForbiddenException("APIキーに権限がないもしくは不正なAPIキーです。 URL: " + Authenticator.FilterErrorMessage(url));
				if (((int)response.StatusCode / 100) == 5)
					throw new DmdataException("dmdataでサーバーエラーが発生しています。 StatusCode: " + response.StatusCode);
				return await response.Content.ReadAsStreamAsync();
			}
			catch (TaskCanceledException)
			{
				throw new DmdataApiTimeoutException("dmdataへのリクエストにタイムアウトしました。 URL: " + Authenticator.FilterErrorMessage(url));
			}
		}
		/// <summary>
		/// 電文のstringを取得する
		/// <para>各電文の種類に合わせた権限が必要です</para>
		/// </summary>
		/// <param name="telegramKey">取得する電文のID</param>
		/// <param name="encoding">stringにする際のエンコード nullの場合UTF8</param>
		/// <returns>レスポンスのStream</returns>
		public async Task<string> GetTelegramStringAsync(string telegramKey, Encoding? encoding = null)
		{
			using var stream = await GetTelegramStreamAsync(telegramKey);
			using var memoryStream = new MemoryStream();

			await stream.CopyToAsync(memoryStream);

			return (encoding ?? Encoding.UTF8).GetString(memoryStream.ToArray());
		}

		/// <summary>
		/// 地震観測地点の情報を取得します
		/// </summary>
		/// <returns>地震観測地点の情報</returns>
		[Obsolete]
		public Task<EarthquakeStationParameterResponse> GetEarthquakeStationParameterAsync()
			=> GetJsonObject<EarthquakeStationParameterResponse>("https://api.dmdata.jp/parameters/v1/earthquake/station.json");

		/// <summary>
		/// 津波観測地点の情報を取得します
		/// </summary>
		/// <returns>津波観測地点の情報</returns>
		[Obsolete]
		public Task<TsunamiStationParameterResponse> GetTsunamiStationParameterAsync()
			=> GetJsonObject<TsunamiStationParameterResponse>("https://api.dmdata.jp/parameters/v1/tsunami/station.json");
	}
}
