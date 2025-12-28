using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DmdataSharp.Authentication;
using DmdataSharp.ApiParameters.V2;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataV2ApiClientのHTTPリクエスト内容検証テスト
/// </summary>
public class DmdataV2ApiClientRequestTests
{
    private readonly List<HttpRequestMessage> _capturedRequests = new();
    private readonly Mock<Authenticator> _mockAuthenticator;
    private readonly HttpClient _httpClient;
    private readonly DmdataV2ApiClient _apiClient;

    public DmdataV2ApiClientRequestTests()
    {
        _mockAuthenticator = new Mock<Authenticator>();
        
        // Authenticatorのモックセットアップ - リクエストをキャプチャしてから例外を投げる
        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(
            It.IsAny<HttpRequestMessage>(),
            It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>(
				(request, next) =>
				{
					_capturedRequests.Add(request);
					// エラーを投げることでリクエストの処理を中断し、リクエスト内容のみをテスト
					throw new InvalidOperationException("Test completed - request captured");
				});
        
        _mockAuthenticator.Setup(auth => auth.FilterErrorMessage(It.IsAny<string>()))
            .Returns<string>(msg => msg);
            
        _httpClient = new HttpClient();
        _apiClient = new DmdataV2ApiClient(_httpClient, _mockAuthenticator.Object);
    }

    [Fact(DisplayName = "GetContractListAsyncで正しいURLにGETリクエストが送信される")]
    public async Task GetContractListAsync_SendsCorrectGetRequest()
    {
        // Arrange & Act
        try
        {
            await _apiClient.GetContractListAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().Be("https://api.dmdata.jp/v2/contract");
    }

    [Fact(DisplayName = "GetSocketListAsyncで正しいURLにGETリクエストが送信される")]
    public async Task GetSocketListAsync_SendsCorrectGetRequest()
    {
        // Arrange & Act
        try
        {
            await _apiClient.GetSocketListAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().Be("https://api.dmdata.jp/v2/socket");
    }

    [Fact(DisplayName = "GetTelegramListAsyncでパラメータが正しくクエリ文字列に変換される")]
    public async Task GetTelegramListAsync_ConvertsParametersToQueryString()
    {
        // Arrange & Act
        try
        {
            await _apiClient.GetTelegramListAsync(type: "VXSE53", xmlReport: true, limit: 50);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().StartWith("https://api.dmdata.jp/v2/telegram?");
        request.RequestUri!.ToString().Should().Contain("type=VXSE53");
        request.RequestUri!.ToString().Should().Contain("xmlReport=true");
        request.RequestUri!.ToString().Should().Contain("limit=50");
    }

    [Fact(DisplayName = "GetEarthquakeEventsAsyncで日付パラメータが正しい形式でクエリ文字列に変換される")]
    public async Task GetEarthquakeEventsAsync_FormatsDateParameterCorrectly()
    {
        // Arrange
        var testDate = new DateTime(2024, 1, 15, 10, 30, 45); // 時刻部分は無視される
        
        // Act
        try
        {
            await _apiClient.GetEarthquakeEventsAsync(date: testDate);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().StartWith("https://api.dmdata.jp/v2/gd/earthquake?");
        request.RequestUri!.ToString().Should().Contain("date=2024-01-15");
    }

    [Fact(DisplayName = "GetSocketStartAsyncでPOSTリクエストが正しく送信される")]
    public async Task GetSocketStartAsync_SendsCorrectPostRequest()
    {
        // Arrange
        var parameter = new SocketStartRequestParameter();
        
        // Act
        try
        {
            await _apiClient.GetSocketStartAsync(parameter);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri!.ToString().Should().Be("https://api.dmdata.jp/v2/socket");
        request.Content.Should().NotBeNull();
    }

    [Fact(DisplayName = "CloseSocketAsyncでDELETEリクエストが正しく送信される")]
    public async Task CloseSocketAsync_SendsCorrectDeleteRequest()
    {
        // Arrange
        var socketId = 123;
        
        // Act
        try
        {
            await _apiClient.CloseSocketAsync(socketId);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Delete);
        request.RequestUri!.ToString().Should().Be($"https://api.dmdata.jp/v2/socket/{socketId}");
    }

    [Fact(DisplayName = "カスタムベースURLが正しく使用される")]
    public async Task CustomBaseUrl_UsedCorrectly()
    {
        // Arrange
        var customApiBaseUrl = "https://customapidmdatajp";
        var customDataApiBaseUrl = "https://customdataapidmdatajp";
        var customClient = new DmdataV2ApiClient(_httpClient, _mockAuthenticator.Object, customApiBaseUrl, customDataApiBaseUrl);
        
        // Act
        try
        {
            await customClient.GetContractListAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.RequestUri!.ToString().Should().Be($"{customApiBaseUrl}/v2/contract");
    }

    [Fact(DisplayName = "GetEarthquakeStationParameterAsyncで正しいURLにGETリクエストが送信される")]
    public async Task GetEarthquakeStationParameterAsync_SendsCorrectGetRequest()
    {
        // Arrange & Act
        try
        {
            await _apiClient.GetEarthquakeStationParameterAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().Be("https://api.dmdata.jp/v2/parameter/earthquake/station");
    }

    [Fact(DisplayName = "GetEarthquakeEventAsyncでイベントIDが正しくURLに含まれる")]
    public async Task GetEarthquakeEventAsync_IncludesEventIdInUrl()
    {
        // Arrange
        var eventId = "20240101123000_0_1";
        
        // Act
        try
        {
            await _apiClient.GetEarthquakeEventAsync(eventId);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Test completed - request captured")
        {
            // 期待される例外
        }

        // Assert
        _capturedRequests.Should().HaveCount(1);
        var request = _capturedRequests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri!.ToString().Should().Be($"https://api.dmdata.jp/v2/gd/earthquake/{eventId}");
    }
}