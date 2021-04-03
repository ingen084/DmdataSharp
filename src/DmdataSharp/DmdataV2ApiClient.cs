using DmdataSharp.ApiParameters.V2;
using DmdataSharp.ApiResponses.V2;
using DmdataSharp.ApiResponses.V2.Parameters;
using DmdataSharp.Authentication;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DmdataSharp
{
	/// <summary>
	/// dmdata API V2 のクライアント
	/// </summary>
	public class DmdataV2ApiClient : DmdataV1ApiClient
	{
		/// <summary>
		/// dmdataのAPI V2クライアントを初期化します
		/// </summary>
		/// <param name="client">内部で使用するHttpClient</param>
		/// <param name="authenticator">使用する認証</param>
		public DmdataV2ApiClient(HttpClient client, Authenticator authenticator) : base(client, authenticator) { }

		/// <summary>
		/// 契約中、未契約の情報リストを取得する
		/// <para>contract.list が必要です</para>
		/// </summary>
		/// <returns>契約中、未契約の情報リスト</returns>
		public Task<ContractListResponse> GetContractListAsync()
			=> GetJsonObject<ContractListResponse>("https://api.dmdata.jp/v2/contract");

		/// <summary>
		/// WebSocketに関するリストを取得する
		/// <para>socket.list が必要です</para>
		/// </summary>
		/// <returns>WebSocketに関するリスト</returns>
		public Task<SocketListResponse> GetSocketListAsync()
			=> GetJsonObject<SocketListResponse>("https://api.dmdata.jp/v2/socket");
		/// <summary>
		/// WebSocket接続を開始するためのURLを取得する
		/// <para>socket.start/取得する情報に合わせた各権限が必要です</para>
		/// </summary>
		/// <param name="param">接続開始のためのパラメータ</param>
		/// <returns>リクエスト結果</returns>
		public Task<SocketStartResponse> GetSocketStartAsync(SocketStartRequestParameter param)
			=> PostJsonObject<SocketStartRequestParameter, SocketStartResponse>("https://api.dmdata.jp/v2/socket", param);
		/// <summary>
		/// WebSocketに関するリストを取得する
		/// <para>socket.close が必要です</para>
		/// </summary>
		/// <returns>成功した場合はnull 失敗した場合はレスポンス</returns>
		public Task<SocketCloseResponse?> CloseSocketAsync(int id)
			=> DeleteJsonObject<SocketCloseResponse>("https://api.dmdata.jp/v2/socket/" + id);

		/// <summary>
		/// 電文リストを取得する
		/// <para>telegram.list が必要です</para>
		/// <para>ポーリングする場合は必ずcursorTokenを使用しnextPoolingInterval以上の間隔でリクエストしてください！</para>
		/// </summary>
		/// <param name="type">検索する電文ヘッダ 前方一致</param>
		/// <param name="xmlReport">XML電文のControl/Headを表示するか</param>
		/// <param name="test">訓練･試験等のテスト等電文を取得するか including: テスト等電文を含む、only: テスト等電文のみ</param>
		/// <param name="cursorToken">前回のレスポンスの値を入れると前回以降の新しい情報のみを取得できる</param>
		/// <param name="formatMode">データフォーマットの指定 生電文: raw、JSON化データ: json</param>
		/// <param name="limit">取得する電文数 最大は100</param>
		/// <returns>電文リスト情報</returns>
		public async Task<TelegramListResponse> GetTelegramListAsync(
			string? type = null,
			bool xmlReport = false,
			string test = "no",
			string? cursorToken = null,
			string? formatMode = "raw",
			int limit = 20
			)
		{
			var parameterMap = new Dictionary<string, string?>();
			if (!string.IsNullOrWhiteSpace(type))
				parameterMap["type"] = type;
			if (xmlReport)
				parameterMap["xmlReport"] = "true";
			if (test != "no")
				parameterMap["test"] = "true";
			if (!string.IsNullOrWhiteSpace(cursorToken))
				parameterMap["cursorToken"] = cursorToken;
			if (formatMode != "raw")
				parameterMap["formatMode"] = formatMode;
			if (limit != 20)
				parameterMap["limit"] = limit.ToString();

#pragma warning disable CS8620 // FormUrlEncodedContentが <string?, string?> を要求しているがDictionaryのKeyはNotNullのため警告回避
			return await GetJsonObject<TelegramListResponse>($"https://api.dmdata.jp/v2/telegram?" + await new FormUrlEncodedContent(parameterMap).ReadAsStringAsync());
#pragma warning restore CS8620 // 参照型の NULL 値の許容の違いにより、パラメーターに引数を使用できません。
		}


		/// <summary>
		/// 地震観測地点の情報を取得します
		/// </summary>
		/// <returns>地震観測地点の情報</returns>
		public new Task<EarthquakeStationParameterResponse> GetEarthquakeStationParameterAsync()
			=> GetJsonObject<EarthquakeStationParameterResponse>("https://api.dmdata.jp/v2/parameter/earthquake/station");
		/// <summary>
		/// 津波観測地点の情報を取得します
		/// </summary>
		/// <returns>津波観測地点の情報</returns>
		public new Task<TsunamiStationParameterResponse> GetTsunamiStationParameterAsync()
			=> GetJsonObject<TsunamiStationParameterResponse>("https://api.dmdata.jp/v2/parameter/tsunami/station");
	}
}
