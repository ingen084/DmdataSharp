using FluentAssertions;
using System;
using System.Net.Http;
using Xunit;
using Moq;
using DmdataSharp.Authentication;
using DmdataSharp.ApiParameters.V2;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataV2ApiClientの単体テスト
/// </summary>
public class DmdataV2ApiClientTests
{
    private readonly Mock<Authenticator> _mockAuthenticator;
    private readonly HttpClient _httpClient;

    public DmdataV2ApiClientTests()
    {
        _mockAuthenticator = new Mock<Authenticator>();
        _httpClient = new HttpClient();
    }

    [Fact(DisplayName = "コンストラクタでHttpClientとAuthenticatorを受け取れる")]
    public void Constructor_WithHttpClientAndAuthenticator_ShouldCreateInstance()
    {
        // Act
        var client = new DmdataV2ApiClient(_httpClient, _mockAuthenticator.Object);

        // Assert
        client.Should().NotBeNull();
    }

    [Fact(DisplayName = "コンストラクタでベースURLを含む全パラメータを受け取れる")]
    public void Constructor_WithAllParameters_ShouldCreateInstance()
    {
        // Arrange
        var apiBaseUrl = "https://customapidmdatajp";
        var dataApiBaseUrl = "https://customdataapidmdatajp";

        // Act
        var client = new DmdataV2ApiClient(_httpClient, _mockAuthenticator.Object, apiBaseUrl, dataApiBaseUrl);

        // Assert
        client.Should().NotBeNull();
        client.ApiBaseUrl.Should().Be(apiBaseUrl);
        client.DataApiBaseUrl.Should().Be(dataApiBaseUrl);
    }

    [Fact(DisplayName = "APIクライアントのメソッド呼び出しが例外をスローしない")]
    public void ApiClientMethods_DoNotThrowExceptions()
    {
        // Arrange
        var client = new DmdataV2ApiClient(_httpClient, _mockAuthenticator.Object);
        var testDate = new DateTime(2024, 1, 1, 12, 30, 45);
        var fromDateTime = new DateTime(2024, 1, 1, 10, 0, 0);
        var toDateTime = new DateTime(2024, 1, 2, 10, 0, 0);
        var validEventId = "20240101123000_0_1";
        var validTelegramKey = "20240101123000_0_VXSE53_010000";
        var socketId = 123;
        var parameter = new SocketStartRequestParameter();
        
        // Act & Assert - すべてのメソッド呼び出しが例外をスローしないことを確認
        Action[] testActions = [
            () => { var _ = client.GetTelegramListAsync(); },
            () => { var _ = client.GetTelegramListAsync(type: "VXSE53", xmlReport: true); },
            () => { var _ = client.GetEarthquakeEventsAsync(); },
            () => { var _ = client.GetEarthquakeEventsAsync(date: testDate, hypocenter: "350", maxInt: "5+"); },
            () => { var _ = client.GetEewEventsAsync(); },
            () => { var _ = client.GetEewEventsAsync(datetimeFrom: fromDateTime, datetimeTo: toDateTime, limit: 50); },
            () => { var _ = client.GetEarthquakeEventAsync(validEventId); },
            () => { var _ = client.GetEewEventAsync(validEventId); },
            () => { var _ = client.GetTelegramStreamAsync(validTelegramKey); },
            () => { var _ = client.GetTelegramStringAsync(validTelegramKey, System.Text.Encoding.UTF8); },
            () => { var _ = client.CloseSocketAsync(socketId); },
            () => { var _ = client.GetSocketStartAsync(parameter); },
            () => { var _ = client.GetEarthquakeStationParameterAsync(); },
            () => { var _ = client.GetTsunamiStationParameterAsync(); },
            () => { var _ = client.GetContractListAsync(); },
            () => { var _ = client.GetSocketListAsync(); }
        ];

        foreach (var action in testActions)
        {
            action.Should().NotThrow();
        }
    }
}