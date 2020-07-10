using System;
using System.Collections;
using System.Collections.Generic;

namespace Ex.Utils {

	/// <summary> Utility class for profiling data collection. </summary>
	public class Trender {

		/// <summary> Position in internal array to store at </summary>
		private int at;
		/// <summary> Total number of records taken </summary>
		private int recorded;
		/// <summary> Record data </summary>
		private double[] record;
		/// <summary> Changed without having been recalculated</summary>
		private bool dirty = false;

		private double _average;
		/// <summary> Reports the average value, recalculated if needed. </summary>
		public double average { get { if (dirty) { Recalc(); } return _average; } }

		private double _max;
		/// <summary> Reports the maximum value, recalculated if needed. </summary>
		public double max { get { if (dirty) { Recalc(); } return _max; } }
		private double _min;
		/// <summary> Reports the minimum value, recalculated if needed. </summary>
		public double min { get { if (dirty) { Recalc(); } return _min; } }


		public Trender(int size = 32) {
			record = new double[size];
			at = 0;
			recorded = 0;
			_min = double.MaxValue;
			_max = double.MinValue;
		}

		/// <summary> Records an entry into the trender </summary>
		/// <param name="data"> Data value to record </param>
		public void Record(double data) {
			dirty = true;
			record[at] = data;
			at = (at + 1) % record.Length;
			recorded++;
		}

		/// <summary> Actually recalculates current min/max/avg </summary>
		private void Recalc() {
			double total = 0;
			int cnt = 0;
			_min = double.MaxValue;
			_max = double.MinValue;
			for (int i = 0; i < record.Length; i++) {
				if (i >= recorded) { break; }
				double data = record[i];
				total += data;
				cnt++;
				if (data < _min) { _min = data; }
				if (data > _max) { _max = data; }
			}
			if (cnt > 0) {
				_average = total / (0.0 + cnt);
			} else {
				_average = -1;
			}


			dirty = false;
		}

		public override string ToString() {
			if (dirty) { Recalc(); }
			return string.Format("Average({0:0.000}) Range({1:0.000}, {2:0.000})", average, min, max);
		}

	}
}
