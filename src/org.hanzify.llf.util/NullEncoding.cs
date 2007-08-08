
#region usings

using System;
using System.Text;

#endregion

namespace org.hanzify.llf.util
{
	public class NullEncoding : Encoding
	{
		#region Singleton

		private static NullEncoding _Instance = new NullEncoding();

		public static NullEncoding Instance
		{
			get { return _Instance; }
		}

		private NullEncoding() {}

		#endregion

		public override int GetByteCount(char[] chars, int index, int count)
		{
			return 0;
		}

		public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
		{
			return 0;
		}

		public override int GetCharCount(byte[] bytes, int index, int count)
		{
			return 0;
		}

		public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
		{
			return 0;
		}

		public override int GetMaxByteCount(int charCount)
		{
			return 0;
		}

		public override int GetMaxCharCount(int byteCount)
		{
			return 0;
		}
	}
}