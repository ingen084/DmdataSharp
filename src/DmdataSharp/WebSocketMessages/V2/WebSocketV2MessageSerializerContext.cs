using System.Text.Json.Serialization;

namespace DmdataSharp.WebSocketMessages.V2
{
	[JsonSerializable(typeof(DataWebSocketMessage), GenerationMode = JsonSourceGenerationMode.Metadata)]
	[JsonSerializable(typeof(ErrorWebSocketMessage), GenerationMode = JsonSourceGenerationMode.Metadata)]
	[JsonSerializable(typeof(PingWebSocketMessage))]
	[JsonSerializable(typeof(PongWebSocketMessage), GenerationMode = JsonSourceGenerationMode.Metadata)]
	[JsonSerializable(typeof(StartWebSocketMessage), GenerationMode = JsonSourceGenerationMode.Metadata)]
	[JsonSerializable(typeof(DmdataWebSocketMessage), GenerationMode = JsonSourceGenerationMode.Metadata)]
	internal partial class WebSocketV2MessageSerializerContext : JsonSerializerContext
	{
	}
}
