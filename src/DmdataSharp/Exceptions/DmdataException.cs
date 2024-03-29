﻿using System;

namespace DmdataSharp.Exceptions
{
	/// <summary>
	/// dmdataに関連する例外
	/// </summary>
	public class DmdataException : Exception
	{
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		public DmdataException(string message) : base(message)
		{
		}
		/// <summary>
		/// 例外を初期化する
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public DmdataException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
