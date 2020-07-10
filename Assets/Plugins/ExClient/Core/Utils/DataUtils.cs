using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Ex.Utils {
	public static class DataUtils {

		static Encoding utf8 = Encoding.UTF8;
		static Encoding ascii = Encoding.ASCII;
		/// <summary> Turns a string into a byte[] using ASCII </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by ASCII encoding </returns>
		public static byte[] ToBytes(this string s) { return ascii.GetBytes(s); }
		/// <summary> Turns a string into a byte[] using UTF8 </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by UTF8 encoding </returns>
		public static byte[] ToBytesUTF8(this string s) { return utf8.GetBytes(s); }

		/// <summary> Reads a byte[] as if it is an ASCII encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> ASCII string created from the byte[] </returns>
		public static string GetString(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return ascii.GetString(b, 0, length);
		}

		/// <summary> Reads a byte[] as if it is a UTF8 encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> UTF8 string created from the byte[] </returns>
		public static string GetStringUTF8(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return utf8.GetString(b, 0, length);
		}

		public static void LogEach<T>(this T[] array) { array.LogEach(1); }
		public static void LogEach<T>(this T[] array, int perLine) {
			StringBuilder str = new StringBuilder();

			for (int i = 0; i < array.Length; i++) {
				str.Append(array[i].ToString());
				str.Append(", ");
				if ((1 + i) % perLine == 0) { str.Append('\n'); }
			}
			//Debug.Log(str.ToString());
		}

		//Thanks, William
		/// <summary> Takes any byte[] and preforms a simple hash algorithm over it. </summary>
		/// <param name="data"> byte[] to process </param>
		/// <returns> Simple hash value created from the given byte[] </returns>
		public static int SimpleHash(this byte[] data) {
			if (data != null) {
				int res = data.Length * data.Length * 31337;
				for (int i = 0; i < data.Length; i++) {
					res ^= (data[i] << ((i % 4) * 8));
				}
				return res;
			} else {
				return 0;
			}
		}

		/// <summary> Take a copy of a sub-region of a byte[] </summary>
		/// <param name="array"> byte[] to chop </param>
		/// <param name="size"> maximum size of resulting sub-array </param>
		/// <param name="start"> start index </param>
		/// <returns> sub-region from given byte[], of max length <paramref name="size"/> starting from index <paramref name="start"/> in the original <paramref name="array"/>. </returns>
		public static byte[] Chop(this byte[] array, int size, int start = 0) {
			if (start >= array.Length) { return null; }
			if (size + start > array.Length) {
				size = array.Length - start;
			}
			byte[] chopped = new byte[size];
			for (int i = 0; i < size; i++) {
				chopped[i] = array[i + start];
			}
			return chopped;
		}
	}

	public enum Endianness { Big, Little }

	/// <summary> Holds extensions for <see cref="BinaryReader"/></summary>
	public static class BinaryReaderEx {
		/// <summary> Cached value of <see cref="BitConverter.IsLittleEndian"/>. Does not change over runtime. </summary>
		/// <remarks> Keeping this close should help the cache out... probably... </remarks>
		public static Endianness sysEnd = BitConverter.IsLittleEndian ? Endianness.Little : Endianness.Big;

		/// <summary> Reads an unsigned 64-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static ulong UInt64(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			byte e = reader.ReadByte();
			byte f = reader.ReadByte();
			byte g = reader.ReadByte();
			byte h = reader.ReadByte();
			long i1, i2;

			if (end == Endianness.Big) {
				i1 = (a << 24) | (b << 16) | (c << 8) | (d);
				i2 = (e << 24) | (f << 16) | (g << 8) | (h);

			} else {
				i1 = (h << 24) | (g << 16) | (f << 8) | (e);
				i2 = (d << 24) | (c << 16) | (b << 8) | (a);
			}

			return (ulong)((i1 << 32) | i2);
		}

		/// <summary> Reads a 64-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static long Int64(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			byte e = reader.ReadByte();
			byte f = reader.ReadByte();
			byte g = reader.ReadByte();
			byte h = reader.ReadByte();
			long i1, i2;

			if (end == Endianness.Big) {
				i1 = (a << 24) | (b << 16) | (c << 8) | (d);
				i2 = (e << 24) | (f << 16) | (g << 8) | (h);

			} else {
				i1 = (h << 24) | (g << 16) | (f << 8) | (e);
				i2 = (d << 24) | (c << 16) | (b << 8) | (a);
			}

			return ((i1 << 32) | i2);
		}

		/// <summary> Reads an unsigned 32-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static uint UInt32(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			return (uint)((end == Endianness.Big) ?
				(a << 24) | (b << 16) | (c << 8) | (d) :
				(d << 24) | (c << 16) | (b << 8) | (a));
		}

		/// <summary> Reads a 32-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static int Int32(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			byte c = reader.ReadByte();
			byte d = reader.ReadByte();
			return ((end == Endianness.Big) ?
				(a << 24) | (b << 16) | (c << 8) | (d) :
				(d << 24) | (c << 16) | (b << 8) | (a));
		}

		/// <summary> Reads an unsigned 16-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static ushort UInt16(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			return (ushort)((end == Endianness.Big ?
				(a << 8) | (b) :
				(b << 8) | (a)));

		}

		/// <summary> Reads a 16-bit long integer from the BinaryReader with the given endianness. </summary>
		/// <param name="reader"> BinaryReader to read from </param>
		/// <param name="end"> Endianness to read in </param>
		/// <returns> Read integer value </returns>
		public static short Int16(this BinaryReader reader, Endianness end = Endianness.Little) {
			byte a = reader.ReadByte();
			byte b = reader.ReadByte();
			return (short)((end == Endianness.Big ?
				(a << 8) | (b) :
				(b << 8) | (a)));

		}

	}


	/// <summary> Implements some methods for <see cref="ConcurrentDictionary{TKey, TValue}"/> which it should have. </summary>
	public static class ConcurrentDictionaryEx {
		/// <summary> Acts the same as a <see cref="Dictionary{TKey, TValue}"/>'s Remove method. </summary>
		/// <typeparam name="TKey"> Key type </typeparam>
		/// <typeparam name="TValue"> Value type </typeparam>
		/// <param name="self"> <see cref="ConcurrentDictionary{TKey, TValue}"/> to remove from </param>
		/// <param name="key"> Key to remove </param>
		/// <returns> True, if the key was present. False if it was not present. </returns>
		public static bool Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) {
			return ((IDictionary<TKey, TValue>)self).Remove(key);
		}
		/// <summary> Calls the <see cref="ConcurrentDictionary{TKey, TValue}"/>'s TryRemove method, and discards the out parameter. </summary>
		/// <typeparam name="TKey"> Key type </typeparam>
		/// <typeparam name="TValue"> Value type </typeparam>
		/// <param name="self"> <see cref="ConcurrentDictionary{TKey, TValue}"/> to remove from </param>
		/// <param name="key"> Key to remove </param>
		/// <returns> True, if the key was present. False if it was not present. </returns>
		public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) {
			TValue ignored;
			return self.TryRemove(key, out ignored);
		}
	}


}
