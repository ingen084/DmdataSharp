using DmdataSharp.Interfaces;
using FluentAssertions;
using Moq;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DmdataSharp.Tests;

/// <summary>
/// DmdataV2Socketの切断処理のテスト
/// </summary>
public class DmdataV2SocketTests
{
    [Fact(DisplayName = "受信中に例外が起きてサーバーが閉じる処理に応じなくても、切断として通知され再接続できる状態になる")]
    public async Task ReceiveError_ServerIgnoresClose_NotifiesDisconnected()
    {
        // Arrange
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var serverStop = new CancellationTokenSource();
        var serverTask = RunUnresponsiveServerAsync(listener, serverStop.Token);

        using var socket = new DmdataV2Socket(new Mock<IDmdataV2ApiClient>().Object);
        var disconnectedCount = 0;
        var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        socket.Disconnected += (s, e) =>
        {
            Interlocked.Increment(ref disconnectedCount);
            disconnected.TrySetResult(true);
        };

        try
        {
            // Act
            await socket.ConnectAsync(new Uri($"ws://localhost:{port}/"));

            // Assert
            var completed = await Task.WhenAny(disconnected.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            completed.Should().Be(disconnected.Task, "閉じる処理が終わらなくても切断として通知される必要がある");
            socket.IsConnected.Should().BeFalse("切断の通知後は再接続の判定に使えるよう未接続になっている必要がある");

            await socket.Invoking(s => s.DisconnectAsync()).Should().NotThrowAsync();
            disconnectedCount.Should().Be(1);
        }
        finally
        {
            serverStop.Cancel();
            await serverTask;
        }
    }

    /// <summary>
    /// JSONではないメッセージを1回送ったあと、受信もせず接続も閉じないサーバー
    /// </summary>
    private static async Task RunUnresponsiveServerAsync(TcpListener listener, CancellationToken stopToken)
    {
        using var client = await listener.AcceptTcpClientAsync();
        using var stream = client.GetStream();

        // ハンドシェイク
        var key = await ReadWebSocketKeyAsync(stream);
        using var sha1 = SHA1.Create();
        var accept = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
        var response = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            $"Sec-WebSocket-Accept: {accept}\r\n" +
            "Sec-WebSocket-Protocol: dmdata.v2\r\n\r\n");
        await stream.WriteAsync(response);

        using var webSocket = WebSocket.CreateFromStream(stream, isServer: true, subProtocol: "dmdata.v2", keepAliveInterval: Timeout.InfiniteTimeSpan);
        await webSocket.SendAsync(Encoding.UTF8.GetBytes("not json"), WebSocketMessageType.Text, true, CancellationToken.None);

        // テストが終わるまで何もしない
        try
        {
            await Task.Delay(Timeout.Infinite, stopToken);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task<string> ReadWebSocketKeyAsync(Stream stream)
    {
        var request = new StringBuilder();
        var buffer = new byte[1];
        while (!request.ToString().EndsWith("\r\n\r\n"))
        {
            if (await stream.ReadAsync(buffer) == 0)
                throw new IOException("ハンドシェイクの途中で切断されました");
            request.Append((char)buffer[0]);
        }

        foreach (var line in request.ToString().Split("\r\n"))
        {
            if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                return line["Sec-WebSocket-Key:".Length..].Trim();
        }
        throw new InvalidOperationException("Sec-WebSocket-Key がありません");
    }
}
