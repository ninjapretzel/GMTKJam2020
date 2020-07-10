using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex.Utils.Ext {
	/// <summary> Holds methods for converting types to Hex strings </summary>
	public static class HexExtensions {
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this byte v) { return String.Format("0x{0:X2}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this short v) { return String.Format("0x{0:X4}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this int v) { return String.Format("0x{0:X8}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this long v) { return String.Format("0x{0:X16}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this float v) { return String.Format("0x{0:X8}", Unsafe.Reinterpret<float, int>(v)); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this double v) { return String.Format("0x{0:X16}", Unsafe.Reinterpret<double, long>(v)); }

		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this sbyte v) { return String.Format("0x{0:X2}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this ushort v) { return String.Format("0x{0:X4}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this uint v) { return String.Format("0x{0:X8}", v); }
		/// <summary> Converts number to internal hex representation </summary>
		/// <param name="v"> Value to convert </param>
		/// <returns> 0x formatted hex string representing given number </returns>
		public static string Hex(this ulong v) { return String.Format("0x{0:X16}", v); }

		/// <summary> Converts a byte array to hex string using the given formatting information. </summary>
		/// <param name="bytes"> Bytes to convert </param>
		/// <param name="perGroup"> Number of bytes per line, default = 4</param>
		/// <param name="spacing"> Number of bytes per spacing group, default = 4</param>
		/// <returns> byte[] formatted as a string </returns>
		public static string Hex(this byte[] bytes, int perGroup = 4, int spacing = 4) {
			StringBuilder str = new StringBuilder("");
			for (int i = 0; i < bytes.Length; i++) {
				if (i % perGroup == 0) {
					str.Append((i==0?"0x":"\n0x"));
				} else if (i > 0 && i % spacing == 0) {
					str.Append(' ');
				}
				str.Append(String.Format("{0:X2}", bytes[i]));
			}

			return str.ToString();
		}

	}
}
