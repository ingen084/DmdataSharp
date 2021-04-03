using DmdataSharp;
using DmdataSharp.ApiParameters.V2;
using DmdataSharp.Exceptions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
	class Program
	{
		static async Task Main()
		{
			Console.WriteLine("DmdataのAPIキーを入力してください>");
			var apiKey = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(apiKey))
				apiKey = Environment.GetEnvironmentVariable("DMDATA_APIKEY");

			using var client = DmdataApiClientBuilder.Default
				.UseApiKey(apiKey)
				.UserAgent("DmdataSharp;Example")
				.Referrer(new Uri("http://ingen084.net/"))
				.BuildV2ApiClient();

			try
			{
				// 契約情報を取得
				var contractInfo = await client.GetContractListAsync();
				Console.WriteLine(@$"** 契約情報 **
*課金明細
{string.Join('\n', contractInfo.Items.Select(i => $"  {i.ClassificationName}({i.Classification}) {i.Price.Day}円/日(最大{i.Price.Month}円/月)"))}");
			}
			catch (DmdataForbiddenException)
			{
				Console.WriteLine("APIキーが正しくないか、課金情報の取得ができませんでした。 contract.list 権限が必要です。");
			}
			Console.WriteLine();
			try
			{
				// 電文リストを10件取得してみる
				var telegramList = await client.GetTelegramListAsync(limit: 10);
				Console.WriteLine($"** 電文リスト **\n");
				foreach (var item in telegramList.Items)
				{
					Console.WriteLine($@"** {item.Head.Type}
  Key: {item.Id}");
				}
			}
			catch (DmdataForbiddenException)
			{
				Console.WriteLine("APIキーが正しくないか、電文リストが取得できませんでした。 telegram.list 権限が必要です。");
			}
			Console.WriteLine("WebSocketへの接続を行います。 Enterキーで接続");
			Console.ReadLine();

			using var socket = new DmdataV2Socket(client);
			socket.Connected += (s, e) => Console.WriteLine("EVENT: connected");
			socket.Disconnected += (s, e) => Console.WriteLine("EVENT: disconnected");
			socket.Error += (s, e) => Console.WriteLine("EVENT: error  c:" + e.Code + " e:" + e.Error);
			socket.DataReceived += (s, e) =>
			{
				Console.WriteLine($@"EVENT: data  type: {e.Head.Type} key: {e.Id} valid: {e.Validate()}
      body: {e.GetBodyString().Substring(0, 20)}...");
			};
			await socket.ConnectAsync(new SocketStartRequestParameter(
				TelegramCategoryV1.Earthquake,
				TelegramCategoryV1.Scheduled,
				TelegramCategoryV1.Volcano,
				TelegramCategoryV1.Weather
			)
			{
				AppName = "DmdataSharp;Example",
			});

			Console.ReadLine();
			await socket.DisconnectAsync();
		}
	}
}
