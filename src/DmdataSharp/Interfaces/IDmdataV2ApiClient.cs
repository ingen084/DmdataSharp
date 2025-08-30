using DmdataSharp.ApiParameters.V2;
using DmdataSharp.ApiResponses.V2;
using DmdataSharp.ApiResponses.V2.GroupedData;
using DmdataSharp.ApiResponses.V2.Parameters;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DmdataSharp.Interfaces
{
	/// <summary>
	/// dmdata API V2 のクライアントインターフェイス
	/// </summary>
	public interface IDmdataV2ApiClient : IDmdataApi
	{
		/// <summary>
		/// 契約中、未契約の情報リストを取得する
		/// </summary>
		/// <returns>契約中、未契約の情報リスト</returns>
		Task<ContractListResponse> GetContractListAsync();

		/// <summary>
		/// WebSocketに関するリストを取得する
		/// </summary>
		/// <returns>WebSocketに関するリスト</returns>
		Task<SocketListResponse> GetSocketListAsync();

		/// <summary>
		/// WebSocket接続を開始するためのURLを取得する
		/// </summary>
		/// <param name="param">接続開始のためのパラメータ</param>
		/// <returns>リクエスト結果</returns>
		Task<SocketStartResponse> GetSocketStartAsync(SocketStartRequestParameter param);

		/// <summary>
		/// WebSocketに関するリストを取得する
		/// </summary>
		/// <returns>成功した場合はnull 失敗した場合はレスポンス</returns>
		Task<SocketCloseResponse?> CloseSocketAsync(int id);

		/// <summary>
		/// 電文リストを取得する
		/// </summary>
		/// <param name="type">検索する電文ヘッダ 前方一致</param>
		/// <param name="xmlReport">XML電文のControl/Headを表示するか</param>
		/// <param name="test">訓練･試験等のテスト等電文を取得するか including: テスト等電文を含む、only: テスト等電文のみ</param>
		/// <param name="cursorToken">前回のレスポンスの値を入れると前回以降の新しい情報のみを取得できる</param>
		/// <param name="formatMode">データフォーマットの指定 生電文: raw、JSON化データ: json</param>
		/// <param name="limit">取得する電文数 最大は100</param>
		/// <returns>電文リスト情報</returns>
		Task<TelegramListResponse> GetTelegramListAsync(
			string? type = null,
			bool xmlReport = false,
			string test = "no",
			string? cursorToken = null,
			string? formatMode = "raw",
			int limit = 20
			);

		/// <summary>
		/// 地震イベント一覧を取得します
		/// </summary>
		/// <param name="hypocenter">検索する震央地名コードの3桁の数字</param>
		/// <param name="maxInt">検索する最大震度の下限</param>
		/// <param name="date">検索する地震波検知時刻の日付 時刻部分は使用されません</param>
		/// <param name="cursorToken">前回のレスポンスの値を入れると前回以降の新しい情報のみを取得できる</param>
		/// <param name="limit">返す情報数を指定する 最大は100</param>
		/// <returns>地震イベント一覧</returns>
		Task<EarthquakeListResponse> GetEarthquakeEventsAsync(
			string? hypocenter = null,
			string? maxInt = null,
			DateTime? date = null,
			string? cursorToken = null,
			int? limit = 20
			);

		/// <summary>
		/// 地震イベントの詳細を取得します
		/// </summary>
		/// <param name="eventId">地震情報のEventID</param>
		/// <returns>地震イベントの詳細</returns>
		Task<EarthquakeEventResponse> GetEarthquakeEventAsync(string eventId);

		/// <summary>
		/// 緊急地震速報イベント一覧を取得します
		/// </summary>
		/// <param name="datetimeFrom">検索する最終報発表日時の絞り込みに使う開始日時</param>
		/// <param name="datetimeTo">検索する最終報発表日時の絞り込みに使う終了日時（この時刻は含まない）</param>
		/// <param name="cursorToken">前回のレスポンスの値を入れると前回以降の新しい情報のみを取得できる</param>
		/// <param name="limit">返す情報数を指定する 最大は100</param>
		/// <returns>緊急地震速報イベント一覧</returns>
		Task<EewListResponse> GetEewEventsAsync(
			DateTime? datetimeFrom = null,
			DateTime? datetimeTo = null,
			string? cursorToken = null,
			int? limit = 20
			);

		/// <summary>
		/// 緊急地震速報イベントの詳細を取得します
		/// </summary>
		/// <param name="eventId">緊急地震速報のEventID</param>
		/// <returns>緊急地震速報イベントの詳細</returns>
		Task<EewEventResponse> GetEewEventAsync(string eventId);

		/// <summary>
		/// 地震観測地点の情報を取得します
		/// </summary>
		/// <returns>地震観測地点の情報</returns>
		Task<EarthquakeStationParameterResponse> GetEarthquakeStationParameterAsync();

		/// <summary>
		/// 津波観測地点の情報を取得します
		/// </summary>
		/// <returns>津波観測地点の情報</returns>
		Task<TsunamiStationParameterResponse> GetTsunamiStationParameterAsync();

		/// <summary>
		/// 電文のStreamを取得する
		/// </summary>
		/// <param name="telegramKey">取得する電文のID</param>
		/// <returns>レスポンスのStream</returns>
		Task<Stream> GetTelegramStreamAsync(string telegramKey);

		/// <summary>
		/// 電文のstringを取得する
		/// </summary>
		/// <param name="telegramKey">取得する電文のID</param>
		/// <param name="encoding">stringにする際のエンコード nullの場合UTF8</param>
		/// <returns>レスポンスのStream</returns>
		Task<string> GetTelegramStringAsync(string telegramKey, Encoding? encoding = null);
	}
}