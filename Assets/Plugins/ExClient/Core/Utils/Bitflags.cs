using BakaTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex.Utils {

	/// <summary> Highly packed bitflags for large numbers of flags. 8 Times more efficient packing than <see cref="bool[]"/>. </summary>
	public class Bitflags {

		/// <summary> Array containing flags </summary>
		private List<int> flags;
		/// <summary> Maximum amount of individual bitflags that can be indexed </summary>
		public int Capacity { get; private set; }
		/// <summary> Gets the number of int sized blocks in this bitflags </summary>
		public int Blocks { get { return Capacity / BLOCKSIZE; } }
		/// <summary> Size of a single int block </summary>
		public static readonly int BLOCKSIZE = 8 * sizeof(int);

		/// <summary> Creates a copy of the flags </summary>
		/// <returns> Array of ints containing the flag data. </returns>
		public int[] CopyFlags() {
			int[] copy = new int[flags.Count];
			for (int i = 0; i < copy.Length; i++) { copy[i] = flags[i]; }
			return copy;
		}

		/// <summary> Constructs a bitflags with at least <paramref name="numFlags"/> in capacity. </summary>
		/// <param name="numFlags"> Number of required flags </param>
		public Bitflags(int numFlags) {
			int size = numFlags / BLOCKSIZE + (numFlags % BLOCKSIZE == 0 ? 0 : 1);
			Capacity = size * BLOCKSIZE;
			flags = new List<int>(size);
			for (int i = 0; i < size; i++) { flags.Add(0); }
		}
		/// <summary> Constructs a bitflags with a copy of the given flags </summary>
		/// <param name="data"> Flags to copy </param>
		public Bitflags(int[] data) {
			Capacity = data.Length * BLOCKSIZE;
			flags = new List<int>(Capacity / BLOCKSIZE);
			for (int i = 0; i < data.Length; i++) { flags.Add(data[i]); }
		}

		/// <summary> Expand the Bitflags by one block </summary>
		public void Expand() {
			flags.Add(0);
			Capacity += BLOCKSIZE;
		}

		/// <summary> Might expand the bitflags. Expands if just one more block is needed to have <paramref name="desiredIndex"/> within <see cref="Capacity"/>. </summary>
		/// <param name="desiredIndex"> Index desired to access </param>
		/// <returns> True, if <paramref name="desiredIndex"/> is now valid, false otherwise. </returns>
		public bool MaybeExpand(int desiredIndex) {
			if (desiredIndex < Capacity) { return true; }
			if (desiredIndex < Capacity + BLOCKSIZE) {
				Expand();
				return true;
			}
			return false;
		}

		/// <summary> Gets the int block at the given index in the underlying array. </summary>
		/// <param name="index"> Index to get. Must be inside [0, <see cref="Blocks"/>-1] </param>
		/// <returns> int value at block </returns>
		public int InspectBlock(int index) { return flags[index]; }
		/// <summary> Sets the int block at the given index in the underlying array. </summary>
		/// <param name="index"> Index to set. Must be inside [0, <see cref="Blocks"/>-1] </param>
		/// <param name="value"> int value to set at block </param>
		public void UpdateBlock(int index, int value) { flags[index] = value; }

		/// <summary> Gets or updates a single flag  </summary>
		/// <param name="index"> Absolute index of bit to set or get </param>
		/// <returns> bit value at index </returns>
		public bool this[int index] {
			get {
				if (index < 0 || index >= Capacity) { throw new IndexOutOfRangeException(); }
				int pos = index / BLOCKSIZE;
				int bit = index % BLOCKSIZE;

				return (flags[pos] & (1 << bit)) != 0;
			}
			set {
				if (index < 0 || index >= Capacity) { throw new IndexOutOfRangeException(); }
				int pos = index / BLOCKSIZE;
				int bit = index % BLOCKSIZE;
				int mask = 1 << bit;

				int bits = flags[pos];
				if (value) {
					bits |= mask;
				} else {
					bits &= (~mask);
				}
				flags[pos] = bits;
			}
		}
	}

	public static class Bitflags_Tests {
		public static Bitflags PrepareSomeFlags() {
			Bitflags flags = new Bitflags(Bitflags.BLOCKSIZE * 4);
			flags[0] = true;
			flags[1] = true;

			flags[Bitflags.BLOCKSIZE+0] = true;
			flags[Bitflags.BLOCKSIZE+1] = true;
			flags[Bitflags.BLOCKSIZE+1] = false;

			return flags;
		}
		public static void TestSetGet() {
			{
				var flags = PrepareSomeFlags();

				int block = flags.InspectBlock(0);
				block.ShouldBe(3);

				flags[1] = false;
				block = flags.InspectBlock(0);
				block.ShouldBe(1);

			}
		}

		public static void TestCopy() {
			var flags = PrepareSomeFlags();
			int[] copy = flags.CopyFlags();
			copy.Length.ShouldBe(4);
			copy[0].ShouldBe(3);
			copy[1].ShouldBe(1);

			var flagsFromCopy = new Bitflags(copy);
			flagsFromCopy.Capacity.ShouldBe(flags.Capacity);
			for (int i = 0; i < flags.Capacity; i++) {
				flagsFromCopy[i].ShouldBe(flags[i]);
			}
		}

		public static void TestExpand() {
			var flags = PrepareSomeFlags();
			int capBefore = flags.Capacity;
			int capAfter = capBefore + Bitflags.BLOCKSIZE;
			flags.Capacity.ShouldBe(capBefore);
			Exception e = null;
			try {
				flags[capAfter-5] = true;
			} catch (Exception ee) { e = ee; }
			e.ShouldNotBe(null);

			flags.Expand();
			flags.Capacity.ShouldBe(capAfter);
			flags[capAfter-5] = true;
			flags[capAfter-5].ShouldBe(true);

		}

	}
}
