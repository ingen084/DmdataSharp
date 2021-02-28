# DmdataSharp

dmdata.jp からの情報の取得を楽にするための非公式ライブラリ

![NuGet](https://img.shields.io/nuget/v/DmdataSharp?style=flat-square)

## 情報取得のチュートリアル

実装する際は必ずDM-D.S.Sのドキュメントを読みながら進めてください。

### 1. インスタンスを初期化する

APIを叩くためのインスタンスを作成します。

```cs
// using DmdataSharp;
using var client = new DmdataV1ApiClient(apiKey, "アプリ名");
```

v1系のAPIは `DmdataV1ApiClient` から利用できます。  
コンストラクタの第1引数にはAPIキーを、 **第2引数には自分のアプリ名を入力してください。(APIリクエスト時のUser-Agentになります)**

### 2. 電文リスト取得する

```cs
var telegramList = await client.GetTelegramListAsync(limit: 10);
```

これで最新の電文を10件取得することが可能です。  
レスポンスなどはAPIドキュメントを参考にしてください。

#### 注意

**ポーリングする場合は必ずnewCatchオプションを使用しましょう。**

### 3. 電文を取得する

```cs
using var stream = await client.GetTelegramStreamAsync(key);
```

keyに取得する電文のKeyパラメータを指定します。これでStreamインスタンスが取得可能です。  
メモリ消費削減のためStreamをそのまま返しているため、usingもしくはDisposeを忘れないようにしましょう。

```cs
var telegramString = await client.GetTelegramStringAsync(key);
```

Streamの扱いがめんどくさい人向けにstringに変換する処理を追加したメソッドもあります。

#### 取得からXMLを解析するまでのサンプル

```cs
XDocument document;
XmlNamespaceManager nsManager;

using (var telegramStream = await ApiClient.GetTelegramStreamAsync("電文のKey"))
using (var reader = XmlReader.Create(telegramStream, new XmlReaderSettings { Async = true }))
{
	document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);
	nsManager = new XmlNamespaceManager(reader.NameTable);
}
nsManager.AddNamespace("jmx", "http://xml.kishou.go.jp/jmaxml1/");
// 地震情報の場合以下の追記が必要
// nsManager.AddNamespace("eb", "http://xml.kishou.go.jp/jmaxml1/body/seismology1/");
// nsManager.AddNamespace("jmx_eb", "http://xml.kishou.go.jp/jmaxml1/elementBasis1/");

// XPathを使用して電文のタイトルが取得できる
var title = document.Root.XPathSelectElement("/jmx:Report/jmx:Control/jmx:Title", nsManager)?.Value;
```

## WebSocketに接続する

### 1. クライアントインスタンスを作成する

```cs
using var socket = new DmdataV1Socket(client);
```

1で作成したAPIクライアントを引数にソケットのインスタンスを作成します。

### 2. イベントハンドラを登録する

データを受信した際のイベントハンドラを登録します。

```cs
socket.Connected += (s, e) => Console.WriteLine("EVENT: connected");
socket.Disconnected += (s, e) => Console.WriteLine("EVENT: disconnected");
socket.Error += (s, e) => Console.WriteLine("EVENT: error  c:" + e.Code + " e:" + e.Error);
socket.DataReceived += (s, e) =>
{
	Console.WriteLine($@"EVENT: data  type: {e.Data.Type} key: {e.Key} valid: {e.Validate()} body: {e.GetBodyString().Substring(0, 20)}...");
};
```

#### Connected

接続が完了し、 `start` を受信したときに発火します。  
メッセージの内容をそのまま参照することができます。

#### Disconnected

切断された･切断したときに発火します。

#### Error

`error` を受信したときに発火します。  
メッセージの内容をそのまま参照することができます。

#### DataReceived

`data` を受信したときに発火します。

```cs
public bool Validate()
```

電文が正しいかどうかの検証を行います。  
正しくない場合、大抵のケースはこのライブラリのバグです。

```cs
public Stream GetBodyStream()
```

圧縮されているかなどを自動で判別し展開やデコードを行います。  
こちらも念の為usingもしくはDisposeを忘れないように注意してください。

```cs
public string GetBodyString(Encoding? encoding = null)
```

`GetBodyStream` にstringに変換する処理を追加したものです。

##### XMLを解析するまでのサンプル

GetTelegramを同じノリで取得できます

```cs
XDocument document;
XmlNamespaceManager nsManager;

using (var telegramStream = data.GetBodyStream())
using (var reader = XmlReader.Create(telegramStream, new XmlReaderSettings { Async = true }))
{
	document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);
	nsManager = new XmlNamespaceManager(reader.NameTable);
}
nsManager.AddNamespace("jmx", "http://xml.kishou.go.jp/jmaxml1/");
// 地震情報の場合以下の追記が必要
// nsManager.AddNamespace("eb", "http://xml.kishou.go.jp/jmaxml1/body/seismology1/");
// nsManager.AddNamespace("jmx_eb", "http://xml.kishou.go.jp/jmaxml1/elementBasis1/");

// XPathを使用して電文のタイトルが取得できる
var title = document.Root.XPathSelectElement("/jmx:Report/jmx:Control/jmx:Title", nsManager)?.Value;
```

### 3. 接続を開始する

```cs
await socket.ConnectAsync(new[]
{
	TelegramCategoryV1.Earthquake,
	TelegramCategoryV1.Scheduled,
	TelegramCategoryV1.Volcano,
	TelegramCategoryV1.Weather,
}, "名前");
```

第1引数には受信したい情報のカテゴリを、第2引数には管理画面の `状況` ページで表示される `メモ` の指定が行なえます。(文字数制限に注意)

## 発生する例外について

これらの例外はメッセージにAPIキーが含まれている場合文字の置き換えを行います。

### DmdataForbiddenException

APIキーが不正か、指定された機能を利用するための権限が足りていません。

### DmdataApiTimeoutException

APIをリクエストした際にタイムアウトしました。

### DmdataException

上記の例外が継承している基底クラスです。

また、ネットワークエラーの場合はその状況に合わせた例外が発生します。
