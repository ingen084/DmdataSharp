using DmdataSharp.ApiResponses;
using DmdataSharp.ApiResponses.V1;
using DmdataSharp.Exceptions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V1
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
				throw new DmdataException("WebSocketメッセージが正常にパースできていないためBodyの検証ができません");
			var result = new SHA384Managed().ComputeHash(Convert.FromBase64String(Body));
			return string.Join("", result.Select(r => r.ToString("x2"))) == Key;
		}
		/// <summary>
		/// 展開処理などを行ったbodyのStreamを取得します。
		/// <para>Disposeしてください！</para>
		/// </summary>
		/// <returns></returns>
		public Stream GetBodyStream()
		{
			if (Body is null || Data is null)
				throw new DmdataException("WebSocketメッセージが正常にパースできていないためBodyのStreamを取得できません");
			var memStream = new MemoryStream(Convert.FromBase64String(Body));
			if (!Data.Xml)
				return memStream;
			return new GZipStream(memStream, CompressionMode.Decompress);
		}
		/// <summary>
		/// 展開処理などを行ったbodyを取得します。
		/// </summary>
		/// <param name="encoding">stringにする際のエンコード nullの場合UTF8</param>
		/// <returns></returns>
		public string GetBodyString(Encoding? encoding = null)
		{
			using var stream = GetBodyStream();
			using var memoryStream = new MemoryStream();

			stream.CopyTo(memoryStream);

			return (encoding ?? Encoding.UTF8).GetString(memoryStream.ToArray());
		}
	}
}
