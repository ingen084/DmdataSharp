using FluentAssertions;
using System;
using Xunit;
using DmdataSharp.Exceptions;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataApiClientBuilderの基本的なテスト
/// </summary>
public class SimpleDmdataApiClientBuilderTests
{
    [Fact(DisplayName = "デフォルトビルダーがnullでないことを確認")]
    public void Default_ShouldReturnNotNull()
    {
        // Act
        var builder = DmdataApiClientBuilder.Default;

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact(DisplayName = "ビルダーパターンのメソッドチェーニングが正常に動作する")]
    public void BuilderPattern_MethodChaining_WorksCorrectly()
    {
        // Arrange
        var builder = DmdataApiClientBuilder.Default;
        var userAgent = "TestUserAgent/1.0";
        var apiKey = "test-api-key";
        var timeout = TimeSpan.FromSeconds(30);
        var referrer = new Uri("https://examplecom");
        var apiBaseUrl = "https://customapidmdatajp";
        var dataApiBaseUrl = "https://customdataapidmdatajp";

        // Act - メソッドチェーニングで設定
        var result = builder
            .UserAgent(userAgent)
            .UseApiKey(apiKey)
            .Timeout(timeout)
            .Referrer(referrer)
            .SetApiBaseUrl(apiBaseUrl)
            .SetDataApiBaseUrl(dataApiBaseUrl);

        // Assert - 設定値の確認
        result.Should().BeSameAs(builder);
        builder.HttpClient.Timeout.Should().Be(timeout);
        builder.HttpClient.DefaultRequestHeaders.Referrer.Should().Be(referrer);
        builder.ApiBaseUrl.Should().Be(apiBaseUrl);
        builder.DataApiBaseUrl.Should().Be(dataApiBaseUrl);
    }

    [Fact(DisplayName = "APIベースURLにnullを指定すると例外が発生する")]
    public void SetApiBaseUrl_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = DmdataApiClientBuilder.Default;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.SetApiBaseUrl(null!));
    }

    [Fact(DisplayName = "データAPIベースURLにnullを指定すると例外が発生する")]
    public void SetDataApiBaseUrl_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = DmdataApiClientBuilder.Default;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.SetDataApiBaseUrl(null!));
    }

    [Fact(DisplayName = "認証方法未設定時にBuildV2ApiClientを呼び出すと例外が発生する")]
    public void BuildV2ApiClient_WithoutAuthentication_ThrowsDmdataException()
    {
        // Arrange
        var builder = DmdataApiClientBuilder.Default;

        // Act & Assert
        var exception = Assert.Throws<DmdataException>(builder.BuildV2ApiClient);
        exception.Message.Should().Contain("認証方法が指定されていません");
    }

    [Fact(DisplayName = "完全なAPIクライアントビルドが正常に実行される")]
    public void BuildV2ApiClient_WithCompleteConfiguration_ReturnsValidClient()
    {
        // Arrange & Act
        var client = DmdataApiClientBuilder.Default
            .UserAgent("TestApp/1.0")
            .UseApiKey("test-api-key")
            .Timeout(TimeSpan.FromSeconds(30))
            .SetApiBaseUrl("https://customapidmdatajp")
            .SetDataApiBaseUrl("https://customdataapidmdatajp")
            .BuildV2ApiClient();

        // Assert
        client.Should().NotBeNull();
        client.ApiBaseUrl.Should().Be("https://customapidmdatajp");
        client.DataApiBaseUrl.Should().Be("https://customdataapidmdatajp");
    }
}