namespace DmdataSharp.Redundancy
{
	/// <summary>
	/// 冗長性WebSocket接続の状態を表す
	/// </summary>
	public enum RedundancyStatus
	{
		/// <summary>
		/// すべての接続が切断されている
		/// </summary>
		Disconnected,

		/// <summary>
		/// 一部の接続が失われているが、少なくとも1つは接続されている
		/// </summary>
		Degraded,

		/// <summary>
		/// 設定された最小接続数を満たしているが、一部の接続が失われている
		/// </summary>
		PartiallyConnected,

		/// <summary>
		/// すべての接続が正常に確立されている
		/// </summary>
		FullyConnected
	}
}