using DmdataSharp.ApiResponses;
using DmdataSharp.Exceptions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages
{
	/// <summary>
	/// WebSocketから飛んでくるdataメッセージを表す
	/// </summary>
	public class DataWebSocketMessage : DmdataWebSocketMessage
	{
		/// <summary>
		/// dataメッセージを初期化する
		/// </summary>
		public DataWebSocketMessage()
		{
			Type = "data";
		}

		/// <summary>
		/// 配信区分
		/// </summary>
		[JsonPropertyName("classification")]
		public string? Classification { get; set; }
		/// <summary>
		/// 配信データを区別するハッシュ
		/// </summary>
		[JsonPropertyName("key")]
		public string? Key { get; set; }
		/// <summary>
		/// 電文本体
		/// </summary>
		[JsonPropertyName("body")]
		public string? Body { get; set; }
		/// <summary>
		/// 電文ヘッダ情報
		/// </summary>
		[JsonPropertyName("data")]
		public TelegramData? Data { get; set; }
		/// <summary>
		/// XML電文におけるHead/Controlの情報
		/// </summary>
		[JsonPropertyName("xmlData")]
		public TelegramXmldata? XmlData { get; set; }

		/// <summary>
		/// Bodyを検証します
		/// </summary>
		/// <returns>正しい値か</returns>
		public bool Validate()
		{
			if (Body is null)
				throw new DmdataException("APIレスポンスが正常にパースできていないためBodyの検証ができません");
			var result = new SHA384Managed().ComputeHash(Convert.FromBase64String(Body));
			return string.Join("", result.Select(r => r.ToString("x2"))) == Key;
		}
		/// <summary>
		/// bodyのStreamを取得します。
		/// <para>Disposeしてください！</para>
		/// </summary>
		/// <returns></returns>
		public Stream GetBodyStream()
		{
			if (Body is null || Data is null)
				throw new DmdataException("APIレスポンスが正常にパースできていないためBodyのStreamを取得できません");
			var memStream = new MemoryStream(Convert.FromBase64String(Body));
			if (!Data.Xml)
				return memStream;
			return new GZipStream(memStream, CompressionMode.Decompress);
		}
		/// <summary>
		/// bodyのStreamを取得します。
		/// <para>Disposeしてください！</para>
		/// </summary>
		/// <returns></returns>
		public string GetBodyString()
		{
			using var stream = GetBodyStream();
			using var memoryStream = new MemoryStream();

			stream.CopyToAsync(memoryStream);

			return Encoding.UTF8.GetString(memoryStream.ToArray());
		}
	}
}
