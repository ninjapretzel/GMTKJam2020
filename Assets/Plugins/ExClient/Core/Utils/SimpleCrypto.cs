#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
using UnityEngine;
#else

#endif

using System;
using System.Collections.Generic;

namespace Ex.Utils {

	/// <summary> Crude, Simple RNG </summary>
	/// <remarks> 
	///		<para> Provides both static and instance methods for random number generation. </para>
	///		<para> Static methods can be used to generate random numbers from given information. </para>
	///		<para> An instance of this class represents a stateful sequence of numbers. </para>
	///		<para> Instances can be constructed with a given seed, so they generate the same sequence of numbers. </para>
	///		<para> Otherwise, if not given a seed, they will use the current <see cref="System.DateTime.Now.Ticks"/> as the seed. </para>
	/// </remarks>
	public class SRNG {

		/// <summary> BAAAARF </summary>
		private const long BARF = 0xFFFF8000;
		/// <summary> BRRAAAAAP </summary>
		private const long WETFART = 0x1F2FF3F4;//0x8BADF00D;
		/// <summary> PFFFFFFFT </summary>
		private const long DRYFART = 0x83828180;//0x501D1F1D;

		/// <summary> Hashes a <see cref="System.DateTime"/> by itsd Ticks </summary>
		/// <param name="seed"> <see cref="System.DateTime"/> to hash </param>
		/// <returns> Hash of <paramref name="seed"/> seed's Ticks </returns>
		public static long hash(DateTime seed) { return hash(seed.Ticks); }

		/// <summary> Gets the hash of a given <paramref name="seed"/>. </summary>
		/// <param name="seed"> Value to use as seed </param>
		/// <returns> Mostly randomly distributed value based on the input hash </returns>
		public static long hash(long seed) {
			long a1 = seed << 32;
			long s0 = seed ^ a1;

			long left = (int)s0;
			long right = (int)(s0 >> 32);
			long join = (left << 32) | right;

			s0 = join ^ (s0 << 1);
			long s1 = BARF ^ (s0 >> 1);

			bool wet = ((byte)s0) % 2 == 0;
			long fart = wet ? WETFART : DRYFART;
			fart = s1 ^ fart;

			return fart;
		}

		/// <summary> Generate a seeded int32 value in range [0, <paramref name="max"/>) using the given <paramref name="seed"/>'s Ticks </summary>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <param name="seed"> Seed value </param>
		/// <returns> 'Random' number in range [0, <paramref name="max"/>) </returns>
		public static int Int32(int max, DateTime seed) { return Int32(max, seed.Ticks); }
		/// <summary> Generate a seeded int32 value in range [0, <paramref name="max"/>) using the given <paramref name="seed"/></summary>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <param name="seed"> Seed value </param>
		/// <returns> 'Random' number in range [0, <paramref name="max"/>) </returns>
		public static int Int32(int max, long seed) {
			int i = (int)(hash(seed));
			return (i < 0 ? -i : i) % max;
		}

		/// <summary> Generate a seeded int32 value in range [<paramref name="min"/>, <paramref name="max"/>) using the given <paramref name="seed"/>'s Ticks </summary>
		/// <param name="min"> Lower limit, inclusive. </param>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <param name="seed"> Seed value </param>
		/// <returns> 'Random' number in range [<paramref name="min"/>, <paramref name="max"/>) </returns>
		public static int Int32Range(int min, int max, DateTime seed) { return min + Int32(max - min, seed.Ticks); }

		/// <summary> Generate a seeded int32 value in range [<paramref name="min"/>, <paramref name="max"/>) using the given <paramref name="seed"/>'s Ticks </summary>
		/// <param name="min"> Lower limit, inclusive. </param>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <param name="seed"> Seed value </param>
		/// <returns> 'Random' number in range [<paramref name="min"/>, <paramref name="max"/>) </returns>
		public static int Int32Range(int min, int max, long seed) { return min + Int32(max - min, seed); }

		/// <summary> Generate a floating point number in the range [0, 1) </summary>
		/// <param name="seed"> Seed value to use to generate the floating point value </param>
		/// <returns> float32 number between [0, 1) </returns>
		public static float Float32(long seed) {
			long s = hash(seed);
			int i = (int)(s % (int.MaxValue / 2)); // Leads to more even distribution
			if (i < 0) { i = -i; }
			float f = ((float)i / (int.MaxValue / 2)); // Again, maxvalue/2 leads to more even distribution
			return f;
		}

		/// <summary> Generate a floating point number in the range [0, 1) </summary>
		/// <param name="seed"> DateTime to use as a seed</param>
		/// <returns> Random in range [0, 1)</returns>
		public static float Float32(DateTime seed) { return Float32(seed.Ticks); }

		/// <summary> Generate a floating point number in the range [0, <paramref name="max"/>) </summary>
		/// <param name="max"> Maximum value of range </param>
		/// <param name="seed"> Seed value to use to generate the floating point value </param>
		/// <returns> float32 number between [0, 1) </returns>
		public static float Float32(float max, long seed) { return Float32(seed) * max; }

		/// <summary> Generate a floating point number in the range [<paramref name="min"/>, <paramref name="max"/>) using the given <paramref name="seed"/> </summary>
		/// <param name="min"> Lower linit, inclusive </param>
		/// <param name="max"> Upper limit, not inclusive </param>
		/// <param name="seed"> Seed value </param>
		/// <returns> 'Random' number in range [<paramref name="min"/>, <paramref name="max"/>) </returns>
		public static float Float32Range(float min, float max, long seed) { return min + Float32(max - min, seed); }

		/// <summary> Current seed value of an RNG instance </summary>
		/// <value> Current seed. Cannot be changed externally. </value>
		public long seed { get; set; }

		/// <summary> Basic constructor, uses the current time (DateTime.Now.Ticks) to seed its initial position. </summary>
		public SRNG() { seed = DateTime.UtcNow.Ticks; }
		/// <summary> Seeded constructor. Uses the given <paramref name="seed"/> as the starting point in the sequence. </summary>
		/// <param name="seed"> Value of the starting point of this sequence </param>
		public SRNG(long seed) { this.seed = seed; }

		/// <summary> Gets the next Int32 value from this RNG's sequence. </summary>
		/// <param name="min"> Lower limit, inclusive. </param>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <returns> Value in range [<paramref name="min"/>, <paramref name="max"/>) </returns>
		public int NextInt(int min, int max) { return Int32Range(min, max, nextHash()); }
		/// <summary> Gets the next Int32 value from this RNG's sequence. </summary>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <returns> Value in range [0, <paramref name="max"/>) </returns>
		public int NextInt(int max) { return Int32(max, nextHash()); }

		/// <summary> Gets the next Float32 value from this RNG's sequence. </summary>
		/// <returns> Value in range [0, 1) </returns>
		public float NextFloat() { return Float32(nextHash()); }
		/// <summary> Gets the next Float32 value from this RNG's sequence in a given range. </summary>
		/// <param name="min"> Lower limit, inclusive. </param>
		/// <param name="max"> Upper limit, not inclusive. </param>
		/// <returns> "Random" value in range [<paramref name="min"/>, <paramref name="max"/>) </returns>
		public float NextFloat(float min, float max) { return Float32Range(min, max, nextHash()); }

		/// <summary> Forwards the RNG to its next value, and returns the new seed value. </summary>
		/// <returns> Newly generated seed value. </returns>
		public long nextHash() { return seed = hash(seed); }

	}

	/// <summary> Simple cryptography class </summary>
	public class EncDec {
		//public const byte encKey = 0x2B;
		public const long START = 0x5555555555555555; //unchecked((long)0xDEADCAFEBABEBEEF);
		const char EOT = (char)0x1F;

		long encPos;
		long decPos;


		public EncDec() {
			encPos = START;
			decPos = START;
		}

		public void Reset() {
			encPos = decPos = START;
		}


		/// <summary> Encrypts a segment of a Byte[], in place. </summary>
		/// <param name="message"> Byte[] to encrypt</param>
		/// <param name="index"> Index to begin encryption at. Negative numbers behave like 0. </param>
		/// <param name="length"> Length of Byte[] segment to encrypt. Negative numbers/0 encrypt from index to the end. </param>
		public void EncryptInPlace(byte[] message, int index = -1, int length = -1) {
			if (index < 0) { index = 0; }
			if (length < 1) { length = message.Length - index; }

			for (int i = 0; i < length; i++) {
				byte b = message[index + i];
				bool wasEOT = (b == EOT);
				long hash = SRNG.hash(encPos++);
				byte encDiff = (byte)hash;
				byte encKey = (byte)(hash >> 8);

				b += encDiff;
				b ^= encKey;

				message[index + i] = b;
				if (wasEOT) { encPos = START; }
			}
		}

		/// <summary> Decrypts a segment of a Byte[], in place. </summary>
		/// <param name="message"> Byte[] to decrypt </param>
		/// <param name="index"> Index to begin decryption at. Negative numbers behave like 0. </param>
		/// <param name="length"> Length of Byte[] segment to decrypt. Negative numbers/0 encrypt from index to the end. </param>
		public void DecryptInPlace(byte[] message, int index = -1, int length = -1) {
			if (index < 0) { index = 0; }
			if (length < 01) { length = message.Length - index; }

			for (int i = 0; i < length; i++) {
				byte b = message[index + i];
				long hash = SRNG.hash(decPos++);
				byte encDiff = (byte)hash;
				byte encKey = (byte)(hash >> 8);

				b ^= encKey;
				b -= encDiff;

				message[index + i] = b;
				if (b == EOT) { decPos = START; }
			}
		}

		/// <summary> Encrypts a message </summary>
		/// <param name="message"> Byte[] to encrypt </param>
		/// <param name="index"> Index to begin copy from. Negative numbers behave like 0. </param>
		/// <param name="length"> Length of Byte[] to copy. Negative numbers/0 take the rest of the array. </param>
		/// <returns> Encrypted copy of Byte[] segment </returns>
		public byte[] Encrypt(byte[] message, int index = -1, int length = -1) {
			if (index < 0) { index = 0; }
			if (length < 1) { length = message.Length - index; }

			byte[] enc = new byte[length];
			for (int i = 0; i < length; i++) {
				byte b = (message[index + i]);
				bool wasEOT = (b == EOT);
				long hash = SRNG.hash(encPos++);
				byte encDiff = (byte)hash;
				byte encKey = (byte)(hash >> 8);

				b += encDiff;
				b ^= encKey;

				enc[i] = b;
				if (wasEOT) { encPos = START; }
			}
			return enc;
		}

		/// <summary> Decrypts a message </summary>
		/// <param name="message"> Byte[] to decrypt </param>
		/// <param name="index"> Index to begin copy from. Negative numbers behave like 0. </param>
		/// <param name="length"> Length of characters to copy. Negative numbers/0 take the rest of the array. </param>
		/// <returns> Decrypted copy of byte[] segment </returns>
		public byte[] Decrypt(byte[] message, int index = -1, int length = -1) {
			if (index < 0) { index = 0; }
			if (length < 1) { length = message.Length - index; }

			byte[] dec = new byte[length];
			for (int i = 0; i < length; i++) {
				byte b = message[index + i];
				long hash = SRNG.hash(decPos++);
				byte encDiff = (byte)hash;
				byte encKey = (byte)(hash >> 8);

				b ^= encKey;
				b -= encDiff;

				dec[i] = b;
				if (b == EOT) { decPos = START; }
			}
			return dec;
		}

	}

	/// <summary> Testing class for crypto stuff. IGNORE ME. </summary>
	public static class CRYPTO_TESTING {
		public static class Debug {
			public static void Log(object o, string s = "normal") {
#if UNITY
				UnityEngine.Debug.Log(o);
#else
				//ServerApp.Debug.Log(o, s);
#endif
			}
		}

		public static void RNG_TESTING() {
			SRNG r = new SRNG();
			int numBuckets = 200;
			int[] buckets = new int[numBuckets];

			for (int i = 0; i < 1024 * 1024; i++) {
				float val = r.NextFloat(0, numBuckets);

				buckets[(int)val]++;
			}

			for (int i = 0; i < buckets.Length; i++) {
				Debug.Log("Bucket " + i + ": " + buckets[i]);
			}

		}

		public static long[] TEST_GENERATE_SEQUENCE(int size = 512) {
			long[] vals = new long[size];

			long pos = EncDec.START;
			for (int i = 0; i < size; i++) {
				pos = SRNG.hash(pos);
				vals[i] = pos;
			}

			return vals;
		}

		const char EOT = (char)0x1F;
		public static int[] BYTE_HIST_COUNT(long[] vals) {
			int[] cnts = new int[256];

			foreach (var val in vals) { cnts[(byte)val]++; }

			return cnts;
		}

		public static int[] HIST_COUNT(int[] cnts, int max) {
			int[] freqs = new int[max];
			foreach (var val in cnts) { freqs[val]++; }
			return freqs;
		}

		public static int CALC_MIN_MAX(int[] counts) {
			//Counts should be int[256]
			byte minPos = 0;
			byte maxPos = 0;
			for (int i = 1; i < 256; i++) {
				if (counts[i] < counts[minPos]) { minPos = (byte)i; }
				if (counts[i] > counts[maxPos]) { maxPos = (byte)i; }

			}

			return minPos | maxPos << 8;
		}

		public static void HASH_TESTING() {
			long[] vals = TEST_GENERATE_SEQUENCE(1024 * 1024);
			int[] cnts = BYTE_HIST_COUNT(vals);
			StringBuilder str = "";
			for (int i = 0; i < cnts.Length; i++) {
				str = str + i + ": " + cnts[i] + "\n";
			}
			//var encdec = new EncDec();
			int d = CALC_MIN_MAX(cnts);
			byte min = (byte)d;
			byte max = (byte)(d >> 8);
			str += "\nmin: " + min + " : " + cnts[min];
			str += "\nmax: " + max + " : " + cnts[max];

			if (cnts[max] < 1024) {
				int[] freqs = HIST_COUNT(cnts, cnts[max] + 1);
				str += "\nfrequency data: \n";
				for (int i = 0; i < freqs.Length; i++) {
					if (freqs[i] > 0) {
						str = str + i + ": " + freqs[i] + "\n";
					}
				}
			} else {
				str += "\nfrequency too spread out to be terribly interesting";
			}

			Debug.Log("Generated histogram: ", "purp");
			Debug.Log(str, "cyan");
		}

		public static void ENCRYPTION_TESTING() {
			string original = "the goyim know too much" + EOT
				+ "Everything here is make believe, only a fool would take it seriously." + EOT
				+ "Don't worry, it's just fake news" + EOT
				+ "SPIRIT COOKING" + EOT
				+ "its like annudah shoah" + EOT
				+ "SHUT IT DOWN THE GOYIM KNOW" + EOT
				+ "dissenter is banned" + EOT
				+ "I'm running out of ideas, all I can do is type shitty /pol/ memes" + EOT
				;


			//string[] multiSendDrifting = original.Split(EOT);
			List<byte[]> wew = new List<byte[]>();
			int stringpos = 0;
			SRNG rand = new SRNG();
			while (stringpos < original.Length) {
				int next = stringpos + rand.NextInt(2, 47);
				if (next > original.Length) { next = original.Length; }
				int diff = next - stringpos;
				string cut = original.Substring(stringpos, diff);
				stringpos = next;

				Debug.Log("Cut " + cut, "cyan");
				wew.Add(cut.ToBytesUTF8());
			}

			byte[][] multibytarraydrifting = wew.ToArray();
			EncDec encDec = new EncDec();
			StringBuilder held = "";
			string str;
			int pos = 0;
			Debug.Log("Faking sending bullshit", "purp");
			foreach (var bytes in multibytarraydrifting) {
				byte[] message = encDec.Encrypt(bytes);
				Debug.Log("Encrypting " + message.Length + " bytes");
				message = encDec.Decrypt(message);
				str = message.GetStringUTF8();

				Debug.Log("Sent [" + str.Replace("" + EOT, "<EOT>") + "]", "yellow");

				held += str;
				int index = held.IndexOf(EOT);
				while (index >= 0) {
					string pulled = held.Substring(0, index);
					held = held.Remove(0, index + 1);
					index = held.IndexOf(EOT);

					if (pulled.Length > 0) {
						Debug.Log("Pulled message " + pos + ": " + pulled, "ltorange");
					}
					//TBD: Pass the message to a location for it to be handled, something like the following line
					//Server.main.messages.Enqueue(new Message(this, pulled));
				}
				pos++;
			}
			//*/

		}





	}


}
