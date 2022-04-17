using System.Text.Json.Serialization;

namespace DmdataSharp.ApiResponses.V2
{
	/// <summary>
	/// WebSocket v2 に接続中のWebSocketを終了した結果
	/// </summary>
	public class SocketCloseResponse : DmdataResponse
	{
	}

	[JsonSerializable(typeof(SocketCloseResponse), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class SocketCloseResponseSerializerContext : JsonSerializerContext
	{
	}
}
