using FluentAssertions;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DmdataSharp.Authentication;
using DmdataSharp.Exceptions;
using System.Net;
using DmdataSharp.ApiResponses.V2;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataApiのレスポンスパースとエラー処理テスト
/// </summary>
public class DmdataApiResponseParseTests : IDisposable
{
    private readonly Mock<Authenticator> _mockAuthenticator;
    private readonly DmdataV2ApiClient _apiClient;

    public DmdataApiResponseParseTests()
    {
        _mockAuthenticator = new Mock<Authenticator>();
        var httpClient = new HttpClient();
        _apiClient = new DmdataV2ApiClient(httpClient, _mockAuthenticator.Object);
    }

    [Fact(DisplayName = "正常なContractListレスポンスが正常にパースされる")]
    public async Task GetContractListAsync_ValidResponse_ParsesSuccessfully()
    {
        // Arrange
        var validJson = """
        {
            "responseId": "test-response-123",
            "responseTime": "2023-01-01T12:00:00Z",
            "status": "ok",
            "items": [
                {
                    "id": 123,
                    "planId": 1,
                    "classification": "telegram.weather",
                    "planName": "Professional",
                    "price": {
                        "day": 100,
                        "month": 3000
                    },
                    "start": "2023-01-01T00:00:00Z",
                    "isValid": true,
                    "connectionCounts": 5
                }
            ]
        }
        """;

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(validJson)
				};
				return Task.FromResult(response);
			});

        // Act
        var result = await _apiClient.GetContractListAsync();

        // Assert
        result.Should().NotBeNull();
        result.ResponseId.Should().Be("test-response-123");
        result.Status.Should().Be("ok");
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(123);
        result.Items[0].PlanId.Should().Be(1);
        result.Items[0].Classification.Should().Be("telegram.weather");
        result.Items[0].PlanName.Should().Be("Professional");
    }

    [Fact(DisplayName = "エラーレスポンス（status=error）でDmdataApiErrorExceptionが発生する")]
    public async Task GetContractListAsync_ErrorStatus_ThrowsDmdataApiErrorException()
    {
        // Arrange
        var errorJson = """
        {
            "responseId": "error-response-456",
            "responseTime": "2023-01-01T12:00:00Z",
            "status": "error",
            "error": {
                "code": 400,
                "message": "Invalid request parameters"
            }
        }
        """;

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(errorJson)
				};
				return Task.FromResult(response);
			});

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataApiErrorException>(_apiClient.GetContractListAsync);
        exception.ResponseId.Should().Be("error-response-456");
        exception.ErrorCode.Should().Be(400);
        exception.ErrorMessage.Should().Be("Invalid request parameters");
    }

    [Fact(DisplayName = "HTTPステータス403でDmdataForbiddenExceptionが発生する")]
    public async Task GetContractListAsync_Http403_ThrowsDmdataForbiddenException()
    {
        // Arrange
        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
			});

        _mockAuthenticator.Setup(auth => auth.FilterErrorMessage(It.IsAny<string>()))
            .Returns<string>(url => url);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataForbiddenException>(_apiClient.GetContractListAsync);
        exception.Message.Should().Contain("権限がないもしくは不正な認証情報です");
    }

    [Fact(DisplayName = "HTTPステータス402でDmdataNotValidContractExceptionが発生する")]
    public async Task GetContractListAsync_Http402_ThrowsDmdataNotValidContractException()
    {
        // Arrange
        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.PaymentRequired));
			});

        _mockAuthenticator.Setup(auth => auth.FilterErrorMessage(It.IsAny<string>()))
            .Returns<string>(url => url);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataNotValidContractException>(_apiClient.GetContractListAsync);
        exception.Message.Should().Contain("有効な契約が存在しません");
    }

    [Fact(DisplayName = "HTTPステータス401でDmdataUnauthorizedExceptionが発生する")]
    public async Task GetContractListAsync_Http401_ThrowsDmdataUnauthorizedException()
    {
        // Arrange
        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
			});

        _mockAuthenticator.Setup(auth => auth.FilterErrorMessage(It.IsAny<string>()))
            .Returns<string>(url => url);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataUnauthorizedException>(_apiClient.GetContractListAsync);
        exception.Message.Should().Contain("認証情報が不正です");
    }

    [Fact(DisplayName = "HTTPステータス5xxでDmdataExceptionが発生する")]
    public async Task GetContractListAsync_Http5xx_ThrowsDmdataException()
    {
        // Arrange
        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
			});

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataException>(_apiClient.GetContractListAsync);
        exception.Message.Should().Contain("サーバーエラーが発生しています");
    }

    [Fact(DisplayName = "不正なJSONでJsonExceptionが発生する")]
    public async Task GetContractListAsync_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json content }";

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(invalidJson)
				};
				return Task.FromResult(response);
			});

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(_apiClient.GetContractListAsync);
    }

    [Fact(DisplayName = "nullレスポンスでDmdataExceptionが発生する")]
    public async Task GetContractListAsync_NullResponse_ThrowsDmdataException()
    {
        // Arrange
        var nullJson = "null";

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(nullJson)
				};
				return Task.FromResult(response);
			});

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataException>(_apiClient.GetContractListAsync);
        exception.Message.Should().Contain("APIレスポンスをパースできませんでした");
    }

    [Fact(DisplayName = "HTTPリクエストのキャンセルでDmdataApiTimeoutExceptionが発生する")]
    public async Task GetContractListAsync_RequestCanceled_ThrowsDmdataApiTimeoutException()
    {
        // Arrange
        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>((request, next) =>
			{
				throw new TaskCanceledException("Request was canceled");
			});

        _mockAuthenticator.Setup(auth => auth.FilterErrorMessage(It.IsAny<string>()))
            .Returns<string>(url => url);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DmdataApiTimeoutException>(_apiClient.GetContractListAsync);
    }

    public void Dispose()
    {
        _apiClient?.Dispose();
    }
}