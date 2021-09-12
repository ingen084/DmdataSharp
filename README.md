# DmdataSharp

dmdata.jp からの情報の取得を楽にするための非公式ライブラリ

![NuGet](https://img.shields.io/nuget/v/DmdataSharp?style=flat-square)

## 情報取得のチュートリアル

実装する際は必ずDM-D.S.Sのドキュメントを読みながら進めてください。

### 1. OAuthクライアントを作成する

![img1](https://gyazo.ingen084.net/data/c74a6942c776dc7508d5901f10a98508.png)  
DM-D.S.Sの管理画面からOAuthクライアントを作成します。

![img2](https://gyazo.ingen084.net/data/0572a77450eb79c8c6b79a84e42bfd34.png)  
各項目を埋めます。注意点としては、

- リダイレクトURIは `http://localhost/` を設定してください。
- **URIは厳密に判定されており、大文字が使用できません。**
- 認証周りについては以下のように設定してください。
  - クライアントの種類: `公開`
    - 現状このライブラリは認可コードフロー+機密クライアントに対応していません。
  - 使用するフロー: `認可コードフロー/リフレッシュトークンフロー`


### 2. インスタンスを初期化する

APIを叩くためのインスタンスを作成します。
まずはBuilderを作成し、UserAgentなどを設定しておきます(任意)。

```cs
// using DmdataSharp;
var builder = DmdataApiClientBuilder.Default
    .UserAgent("アプリ名")
    .Referrer(new Uri("リファラにいれるURL"));
```

### 3. 認可を求める

**リフレッシュトークンを保存した場合、この手順はスキップすることができます。**

1で作成したOAuthクライアントIDと許可を求めたい(呼びたいAPIが該当する)スコープを `SimpleOAuthAuthenticator.AuthorizationAsync` に渡して各種トークン(資格情報)の取得を行います。

```cs
// using DmdataSharp.Authentication.OAuth;
var clientId = "クライアントID";
var scopes = new[] { "contract.list", "telegram.list", "socket.start", "telegram.get.earthquake" };
try
{
    // 認可を得る
    var (refreshToken, accessToken, accessTokenExpires) = await SimpleOAuthAuthenticator.AuthorizationAsync(
        builder.HttpClient,
        clientId,
        scopes,
        "DmdataSharp サンプルアプリケーション",
        url => Process.Start(url),
        "http://localhost:{好きなポート}/",
        TimeSpan.FromMinutes(10));

    // 得た資格情報を登録する(4の内容)
    builder = builder.UseOAuthRefreshToken(clientId, scopes, refreshToken, accessToken, accessTokenExpires);
}
catch (Exception ex)
{
    Console.WriteLine("認証に失敗しました\n" + ex);
    return;
}

```

`{好きなポート}` の部分は好みで設定してください。  
後述する 内部でホストするHTTPサーバー で使用します。

`AuthorizationAsync` の解説をしておきます。

```cs
Task<(string refreshToken, string accessToken, DateTime accessTokenExpire)> AuthorizationAsync(
    HttpClient client,      // 内部でAPIを呼ぶ際に使用するHttpClient 今回はBuilderで作成したHttpClientを使用します
    string clientId,        // OAuthクライアントID
    string[] scopes,        // 認可を求めるスコープ
    string title,           // 認可時にブラウザ上に表示されるアプリケーション名
    Action<string> openUrl, // URLが求められた際にブラウザを開くためのデリゲート
    string listenPrefix,    // 内部でホストするHTTPサーバーのオプション 1で設定するリダイレクトURIと同じにしてください
    TimeSpan timeout)       // 認可･戻るボタンが押されなかった場合失敗扱いにするまでの時間
```

#### 内部でホストするHTTPサーバーについて

この認可フローはWebブラウザを使用した方式であるため、認可ボタンを押した後ライブラリ内で建てたHTTPサーバーにリダイレクトすることでトークンの取得を行います。  
ファイアウォールの確認画面が表示されてしまうため、基本 `localhost` でポートもなるべく大きなもの(1024以上)を使用してください。外部に何かを公開してしまうというものではありません。

### 4. 資格情報を登録する

作成していたBuilderに3で取得した資格情報の登録を行います(3のコード内に含まれています)。  
リフレッシュトークンは長期間使用することができるため、起動のたびにブラウザを開かないようにするためにも、アプリケーションに組み込むときは保存しておくとよいでしょう。  

```cs
builder = builder.UseOAuthRefreshToken(clientId, scopes, refreshToken, accessToken, accessTokenExpires);
```

`accessToken` `accessTokenExpires` は必須ではありません(API実行時に自動で更新されます)。

### 5. APIクライアントを作成する

`BuildV2ApiClient` でAPIクライアントを作成します。

```cs
using var client = builder.BuildV2ApiClient();
```

これで各種APIが呼べるようになりました。

**Disposeにアクセストークンの失効が含まれているため、アプリケーションの終了時などにDisposeを忘れないようにしましょう**。

### 6. 電文リストを取得する

```cs
var telegramList = await client.GetTelegramListAsync(limit: 10);
```

これで最新の電文を10件取得することが可能です。  
レスポンスなどはAPIドキュメントを参考にしてください。

#### 注意

**ポーリングする場合は必ず `cursorToken` オプションを使用しましょう。**

### 7. 電文を取得する

```cs
using var stream = await client.GetTelegramStreamAsync(id);
```

keyに取得する電文のKeyパラメータを指定します。これでStreamインスタンスが取得可能です。  
メモリ消費削減のためStreamをそのまま返しているため、usingもしくはDisposeを忘れないようにしましょう。

```cs
var telegramString = await client.GetTelegramStringAsync(id);
```

Streamの扱いがめんどくさい人向けにstringに変換する処理を追加したメソッドもあります。

#### 取得からXMLを解析するまでのサンプル

```cs
XDocument document;
XmlNamespaceManager nsManager;

using (var telegramStream = await ApiClient.GetTelegramStreamAsync("電文のId"))
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

### 8. アプリケーションの連携を解除する

アプリケーションの連携を解除する際はリフレッシュトークンの失効が必要です。

```cs
if (client.Authenticator is OAuthAuthenticator authenticator)
    await authenticator.Credential.RevokeRefreshTokenAsync();
```

(設計が微妙で苦しい処理になってます、ごめんなさい)

## WebSocketに接続する

### 1. クライアントインスタンスを作成する

```cs
using var socket = new DmdataV2Socket(client);
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
    Console.WriteLine($@"EVENT: data  type: {e.Head.Type} key: {e.Id} valid: {e.Validate()} body: {e.GetBodyString().Substring(0, 20)}...");
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
await socket.ConnectAsync(new SocketStartRequestParameter(
    TelegramCategoryV1.Earthquake,
    TelegramCategoryV1.Scheduled,
    TelegramCategoryV1.Volcano,
    TelegramCategoryV1.Weather
)
{
    AppName = "アプリ名",
});
```

`SocketStartRequestParameter` の引数には受信したい情報のカテゴリを、 `AppName` は管理画面の `状況` ページで表示される `メモ` の指定が行なえます。(文字数制限に注意)  
その他にも `Types` で電文のフィルタなども行えますのでご活用ください。

## 発生する例外について

APIキー認証の場合、メッセージにAPIキーが含まれている場合文字の置き換えを行います。

### DmdataAuthenticationException

各種資格情報が失効しているか、認可されませんでした。

### DmdataUnauthorizedException

認証情報が不正です。

### DmdataForbiddenException

認証情報が不正です。  
使用中の資格情報に権限がない場合などに発生します。  
APIv1の場合はAPIキーが不正な場合もこの例外が発生します。

### DmdataApiTimeoutException

APIをリクエストした際にタイムアウトしました。

### DmdataRateLimitExceededException

レートリミットに引っかかりました。  
このライブラリは同時アクセスの制御を行いません。  
しばらく待ってアクセスし直してください。

### DmdataException

上記の例外が継承している基底クラスです。  
いくつかのエラーで使用されています。

また、ネットワークエラーの場合はその状況に合わせた例外が発生します。

## このライブラリで未実装のAPIを使用したい場合

このライブラリには実装されていない、dmdata.jpのAPIを使用したい場合は `DmdataV2ApiClient` を継承したクラスを作ることで拡張可能です。  
`DmdataApiClientBuilder` の `Build` メソッドの型引数に継承したクラスを入れることで既存の機能と同様に初期化することができます。

注意点としては、コンストラクタの引数は継承元のクラスに合わせるようにしてください。  
`protected` なメソッドを使用することでライブラリの認証機能などを使用することができます。詳細は `DmdataV2ApiClient` のコードなどを読んでみてください。
