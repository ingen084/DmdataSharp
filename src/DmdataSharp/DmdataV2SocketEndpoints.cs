namespace DmdataSharp
{
	/// <summary>
	/// WebSocket V2 エンドポイント情報
	/// </summary>
	public static class DmdataV2SocketEndpoints

	{
		/// <summary>
		/// グローバル(東京または大阪)
		/// </summary>
		public const string Global = "ws.api.dmdata.jp";

		/// <summary>
		/// Tokyo Region
		/// </summary>
		public const string Tokyo = "ws-tokyo.api.dmdata.jp";

		/// <summary>
		/// Osaka Region
		/// </summary>
		public const string Osaka = "ws-osaka.api.dmdata.jp";

		/// <summary>
		/// AWS apne1-az4(Tokyo)
		/// </summary>
		public const string Apne1Az4 = "ws001.api.dmdata.jp";

		/// <summary>
		/// AWS apne1-az1(Tokyo)
		/// </summary>
		public const string Apne1Az1 = "ws002.api.dmdata.jp";

		/// <summary>
		/// AWS apne3-az3(Osaka)
		/// </summary>
		public const string Apne3Az3 = "ws003.api.dmdata.jp";

		/// <summary>
		/// AWS apne3-az1(Osaka)
		/// </summary>
		public const string Apne3Az1 = "ws004.api.dmdata.jp";
	}
}
