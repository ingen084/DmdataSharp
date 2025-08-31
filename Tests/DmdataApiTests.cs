using FluentAssertions;
using System;
using System.Net.Http;
using System.Threading;
using Xunit;
using Moq;
using DmdataSharp.Authentication;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataApiベースクラスの単体テスト
/// </summary>
public class DmdataApiTests : IDisposable
{
    private readonly Mock<Authenticator> _mockAuthenticator;
    private readonly HttpClient _httpClient;
    private readonly TestDmdataApiClient _testClient;

    public DmdataApiTests()
    {
        _mockAuthenticator = new Mock<Authenticator>();
        _httpClient = new HttpClient();
        _testClient = new TestDmdataApiClient(_httpClient, _mockAuthenticator.Object);
    }

    [Fact(DisplayName = "コンストラクタでHttpClientとAuthenticatorが設定される")]
    public void Constructor_WithHttpClientAndAuthenticator_SetsProperties()
    {
        // Arrange & Act
        var client = new TestDmdataApiClient(_httpClient, _mockAuthenticator.Object);

        // Assert
        client.HttpClient.Should().BeSameAs(_httpClient);
        client.Authenticator.Should().BeSameAs(_mockAuthenticator.Object);
        client.ApiBaseUrl.Should().Be("https://api.dmdata.jp");
        client.DataApiBaseUrl.Should().Be("https://data.api.dmdata.jp");
        client.AllowPararellRequest.Should().BeFalse();
    }

    [Fact(DisplayName = "コンストラクタでカスタムベースURLが設定される")]
    public void Constructor_WithCustomBaseUrls_SetsCustomUrls()
    {
        // Arrange
        var customApiBaseUrl = "https://customapidmdatajp";
        var customDataApiBaseUrl = "https://customdataapidmdatajp";

        // Act
        var client = new TestDmdataApiClient(_httpClient, _mockAuthenticator.Object, customApiBaseUrl, customDataApiBaseUrl);

        // Assert
        client.ApiBaseUrl.Should().Be(customApiBaseUrl);
        client.DataApiBaseUrl.Should().Be(customDataApiBaseUrl);
    }

    [Fact(DisplayName = "APIキーコンストラクタで正常に初期化される")]
    public void Constructor_WithApiKey_InitializesCorrectly()
    {
        // Arrange
        var apiKey = "test-api-key";
        var userAgent = "TestApp/1.0";
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        var client = new TestDmdataApiClient(apiKey, userAgent, timeout);

        // Assert
        client.Timeout.Should().Be(timeout);
        client.Authenticator.Should().BeOfType<ApiKeyAuthenticator>();
    }

    [Fact(DisplayName = "APIキーコンストラクタでnullを指定すると例外が発生する")]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        // Arrange
        var userAgent = "TestApp/1.0";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestDmdataApiClient(null!, userAgent));
    }

    [Fact(DisplayName = "UserAgentがnullの場合例外が発生する")]
    public void Constructor_WithNullUserAgent_ThrowsArgumentNullException()
    {
        // Arrange
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestDmdataApiClient(apiKey, null!));
    }

    [Fact(DisplayName = "Timeoutプロパティが正常に設定・取得される")]
    public void Timeout_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var newTimeout = TimeSpan.FromSeconds(30);

        // Act
        _testClient.Timeout = newTimeout;

        // Assert
        _testClient.Timeout.Should().Be(newTimeout);
        _testClient.HttpClient.Timeout.Should().Be(newTimeout);
    }

    [Fact(DisplayName = "ApiBaseUrlプロパティが正常に設定・取得される")]
    public void ApiBaseUrl_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var newApiBaseUrl = "https://newapidmdatajp";

        // Act
        _testClient.ApiBaseUrl = newApiBaseUrl;

        // Assert
        _testClient.ApiBaseUrl.Should().Be(newApiBaseUrl);
    }

    [Fact(DisplayName = "DataApiBaseUrlプロパティが正常に設定・取得される")]
    public void DataApiBaseUrl_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var newDataApiBaseUrl = "https://newdataapidmdatajp";

        // Act
        _testClient.DataApiBaseUrl = newDataApiBaseUrl;

        // Assert
        _testClient.DataApiBaseUrl.Should().Be(newDataApiBaseUrl);
    }

    [Fact(DisplayName = "AllowPararellRequestプロパティが正常に設定・取得される")]
    public void AllowPararellRequest_SetAndGet_WorksCorrectly()
    {
        // Arrange & Act
        _testClient.AllowPararellRequest = true;

        // Assert
        _testClient.AllowPararellRequest.Should().BeTrue();
    }

    [Fact(DisplayName = "Authenticatorプロパティが正常に設定・取得される")]
    public void Authenticator_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var newMockAuthenticator = new Mock<Authenticator>();

        // Act
        _testClient.Authenticator = newMockAuthenticator.Object;

        // Assert
        _testClient.Authenticator.Should().BeSameAs(newMockAuthenticator.Object);
    }


    [Fact(DisplayName = "Disposeでリソースが正常に解放される")]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var mockAuth = new Mock<Authenticator>();
        var httpClient = new HttpClient();
        var client = new TestDmdataApiClient(httpClient, mockAuth.Object);

        // Act & Assert
        client.Dispose();
        mockAuth.Verify(auth => auth.Dispose(), Times.Once);
        
        // 複数回のDisposeが例外を発生させないことを確認
        Action disposeAction = client.Dispose;
        disposeAction.Should().NotThrow();
    }


    public void Dispose()
    {
        _testClient?.Dispose();
        _httpClient?.Dispose();
    }

    /// <summary>
    /// テスト用のDmdataApi実装クラス
    /// </summary>
    private class TestDmdataApiClient : DmdataApi
    {
        public TestDmdataApiClient(HttpClient httpClient, Authenticator authenticator) 
            : base(httpClient, authenticator) { }

        public TestDmdataApiClient(HttpClient httpClient, Authenticator authenticator, string apiBaseUrl, string dataApiBaseUrl) 
            : base(httpClient, authenticator, apiBaseUrl, dataApiBaseUrl) { }

        public TestDmdataApiClient(string apiKey, string userAgent, TimeSpan? timeout = null) 
            : base(apiKey, userAgent, timeout) { }

        // protectedメンバーをテスト用にパブリックに公開
        public new HttpClient HttpClient => base.HttpClient;
        public new ManualResetEventSlim RequestMre => base.RequestMre;

    }

}