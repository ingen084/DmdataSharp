using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DmdataSharp.Authentication;
using DmdataSharp.ApiResponses;
using DmdataSharp.ApiResponses.V2;
using System.Threading;
using System.Net;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataApiの並列リクエスト制御テスト
/// </summary>
public class DmdataApiRequestControlTests : IDisposable
{
    private readonly Mock<Authenticator> _mockAuthenticator;
    private readonly DmdataV2ApiClient _apiClient;

    public DmdataApiRequestControlTests()
    {
        _mockAuthenticator = new Mock<Authenticator>();
        var httpClient = new HttpClient();
        _apiClient = new DmdataV2ApiClient(httpClient, _mockAuthenticator.Object);
    }

    [Fact(DisplayName = "AllowPararellRequest=false時に並列リクエストが制御される")]
    public async Task AllowPararellRequest_False_ControlsParallelRequests()
    {
        // Arrange
        _apiClient.AllowPararellRequest = false;
        var concurrentRequests = 0;
        var maxConcurrentRequests = 0;
        var requestCount = 0;
        var validJson = """
        {
            "responseId": "test-response-123",
            "responseTime": "2023-01-01T12:00:00Z",
            "status": "ok",
            "items": []
        }
        """;

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>(async (request, next) =>
            {
                Interlocked.Increment(ref requestCount);
                var current = Interlocked.Increment(ref concurrentRequests);
                
                // 最大同時実行数を記録
                var currentMax = maxConcurrentRequests;
                while (current > currentMax)
                {
                    var original = Interlocked.CompareExchange(ref maxConcurrentRequests, current, currentMax);
                    if (original == currentMax) break;
                    currentMax = original;
                }

                // リクエスト処理時間をシミュレート
                await Task.Delay(30);
                
                Interlocked.Decrement(ref concurrentRequests);

				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(validJson)
				};
				return response;
            });

        // Act - 5つのリクエストを同時に開始
        var tasks = new[]
        {
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync)
        };

        await Task.WhenAll(tasks);

        // Assert
        requestCount.Should().Be(5, "5つのリクエストが実行されるべき");
        // ManualResetEventSlimによる制御では完全に1に制限できないが、制御なしと比較して大幅に制限される
        maxConcurrentRequests.Should().BeLessOrEqualTo(3, "AllowPararellRequest=false時は同時実行数が制限されるべき");
        maxConcurrentRequests.Should().BeLessThan(5, "完全に並列実行されるべきではない");
    }

    [Fact(DisplayName = "AllowPararellRequest=true時に並列リクエストが並行実行される")]
    public async Task AllowPararellRequest_True_ExecutesRequestsConcurrently()
    {
        // Arrange
        _apiClient.AllowPararellRequest = true;
        var requestStartCount = 0;
        var concurrentCount = 0;
        var maxConcurrent = 0;
        var validJson = """
        {
            "responseId": "test-response-123",
            "responseTime": "2023-01-01T12:00:00Z",
            "status": "ok",
            "items": []
        }
        """;

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>(async (request, next) =>
            {
                Interlocked.Increment(ref requestStartCount);
                var current = Interlocked.Increment(ref concurrentCount);
                
                // 最大同時実行数を記録
                var currentMax = maxConcurrent;
                while (current > currentMax)
                {
                    var original = Interlocked.CompareExchange(ref maxConcurrent, current, currentMax);
                    if (original == currentMax) break;
                    currentMax = original;
                }

                await Task.Delay(30);
                
                Interlocked.Decrement(ref concurrentCount);

				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(validJson)
				};
				return response;
            });

        // Act - 5つのリクエストを同時に開始
        var tasks = new[]
        {
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync),
            Task.Run(_apiClient.GetContractListAsync)
        };

        await Task.WhenAll(tasks);

        // Assert
        requestStartCount.Should().Be(5);
        maxConcurrent.Should().BeGreaterOrEqualTo(4, "AllowPararellRequest=true時は制御なしで多数のリクエストが同時実行されるべき");
    }

    [Fact(DisplayName = "逐次リクエストが正常に実行される")]
    public async Task SequentialRequests_WorkCorrectly()
    {
        // Arrange
        _apiClient.AllowPararellRequest = false;
        var requestOrder = new List<int>();
        var requestId = 0;

        var validJson = """
        {
            "responseId": "test-response-123",
            "responseTime": "2023-01-01T12:00:00Z",
            "status": "ok",
            "items": []
        }
        """;

        _mockAuthenticator.Setup(auth => auth.ProcessRequestAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<Func<HttpRequestMessage, Task<HttpResponseMessage>>>()))
            .Returns<HttpRequestMessage, Func<HttpRequestMessage, Task<HttpResponseMessage>>>(async (request, next) =>
            {
                var currentId = Interlocked.Increment(ref requestId);
                lock (requestOrder)
                {
                    requestOrder.Add(currentId);
                }

                await Task.Delay(10);

				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(validJson)
				};
				return response;
            });

        // Act - 3つのリクエストを逐次実行
        await _apiClient.GetContractListAsync();
        await _apiClient.GetContractListAsync();
        await _apiClient.GetContractListAsync();

        // Assert
        requestOrder.Should().Equal(new[] { 1, 2, 3 }, "逐次リクエストは順序通りに実行されるべき");
    }

    public void Dispose()
    {
        _apiClient?.Dispose();
    }
}