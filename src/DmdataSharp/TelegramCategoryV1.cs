using System;

namespace DmdataSharp
{
	/// <summary>
	/// 配信区分
	/// </summary>
	public enum TelegramCategoryV1
	{
		/// <summary>
		/// 地震・津波関連
		/// </summary>
		Earthquake = 0,
		/// <summary>
		/// 火山関連
		/// </summary>
		Volcano,
		/// <summary>
		/// 気象警報･注意報関連
		/// </summary>
		Weather,
		/// <summary>
		/// 定時関連
		/// </summary>
		Scheduled,
	}

	/// <summary>
	/// TelegramCategoryの拡張メソッドを定義するクラス
	/// </summary>
	public static class TelegramCategoryExtensions
	{
		/// <summary>
		/// パラメータで使用する形式に変換する
		/// </summary>
		/// <param name="cat">変換元</param>
		/// <returns></returns>
		public static string ToParameterString(this TelegramCategoryV1 cat)
			=> cat switch
			{
				TelegramCategoryV1.Earthquake => "telegram.earthquake",
				TelegramCategoryV1.Volcano => "telegram.volcano",
				TelegramCategoryV1.Weather => "telegram.weather",
				TelegramCategoryV1.Scheduled => "telegram.scheduled",
				_ => throw new ArgumentException("存在しないパラメータを変換しようとしました", nameof(cat)),
			};
	}
}
