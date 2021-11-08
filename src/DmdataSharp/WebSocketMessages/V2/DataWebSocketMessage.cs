using DmdataSharp.ApiResponses.V2;
using DmdataSharp.Exceptions;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V2
{
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
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
		/// バージョンを示す
		/// <para>作成処理の変更で予告なく変更となる場合がある</para>
		/// </summary>
		[JsonPropertyName("version")]
		public string Version { get; set; }
		/// <summary>
		/// 配信データを区別するユニーク384bitハッシュ
		/// </summary>
		[JsonPropertyName("id")]
		public string Id { get; set; }
		/// <summary>
		/// 配信区分
		/// </summary>
		[JsonPropertyName("classification")]
		public string Classification { get; set; }
		/// <summary>
		/// 通過情報
		/// </summary>
		[JsonPropertyName("passing")]
		public PassingInfo[] Passing { get; set; }
		/// <summary>
		/// 電文ヘッダ情報
		/// </summary>
		[JsonPropertyName("head")]
		public TelegramListResponse.Head Head { get; set; }
		/// <summary>
		/// XML電文におけるHead/Controlの情報
		/// </summary>
		[JsonPropertyName("xmlReport")]
		public TelegramXmldata? XmlReport { get; set; }
		/// <summary>
		/// bodyプロパティの圧縮形式を示す
		/// <para>"gzip"または"zip"、非圧縮時はnull</para>
		/// </summary>
		[JsonPropertyName("compression")]
		public string? Compression { get; set; }
		/// <summary>
		/// bodyプロパティのエンコーディング形式を示す
		/// <para>"base64"または"utf-8"</para>
		/// </summary>
		[JsonPropertyName("encoding")]
		public string? Encoding { get; set; }
		/// <summary>
		/// 電文本体
		/// </summary>
		[JsonPropertyName("body")]
		public string Body { get; set; }

		/// <summary>
		/// 電文の通過情報
		/// </summary>
		public class PassingInfo
		{
			/// <summary>
			/// 通過場所の名前
			/// </summary>
			[JsonPropertyName("name")]
			public string Name { get; set; }
			/// <summary>
			/// 通過した時間
			/// </summary>
			[JsonPropertyName("time")]
			public DateTime Time { get; set; }

		}

		/// <summary>
		/// Bodyを生のbyte[]に変換した値を取得する
		/// </summary>
		/// <returns></returns>
		public byte[] GetRawBodyValue()
		{
			if (Encoding == "base64")
				return Convert.FromBase64String(Body);
			return System.Text.Encoding.UTF8.GetBytes(Body);
		}
		/// <summary>
		/// Bodyを検証する
		/// </summary>
		/// <returns>正しい値か</returns>
		public bool Validate()
		{
			if (Body is null)
				throw new DmdataException("WebSocketメッセージが正常にパースできていないためBodyの検証ができません");
			var result = SHA384.Create().ComputeHash(GetRawBodyValue());
			return string.Join("", result.Select(r => r.ToString("x2"))) == Id;
		}
		/// <summary>
		/// 展開処理などを行ったbodyのStreamを取得する
		/// <para>Disposeしてください！</para>
		/// </summary>
		/// <returns></returns>
		public Stream GetBodyStream()
		{
			if (Body is null || Head is null)
				throw new DmdataException("WebSocketメッセージが正常にパースできていないためBodyのStreamを取得できません");
			var memStream = new MemoryStream(Convert.FromBase64String(Body));
			switch (Compression)
			{
				// 気象庁から送られてくるzipファイル、特に断りのない場合ファイルの中身は1つらしい
				case "zip":
					var archive = new ZipArchive(memStream, ZipArchiveMode.Read);
					if (archive.Entries.Count != 1)
						throw new DmdataException("zip内のファイル数が不正です: " + archive.Entries.Count);
					return archive.Entries[0].Open();
				// gzipによる圧縮
				case "gzip":
					return new GZipStream(memStream, CompressionMode.Decompress);
				default:
					return memStream;
			};
		}
		/// <summary>
		/// 展開処理などを行ったbodyをstringで取得する
		/// </summary>
		/// <param name="encoding">stringにする際のエンコード nullの場合UTF8</param>
		/// <returns></returns>
		public string GetBodyString(Encoding? encoding = null)
		{
			using var stream = GetBodyStream();
			using var memoryStream = new MemoryStream();

			stream.CopyTo(memoryStream);

			return (encoding ?? System.Text.Encoding.UTF8).GetString(memoryStream.ToArray());
		}
	}
}
