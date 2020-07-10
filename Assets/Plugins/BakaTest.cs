using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;

namespace BakaTest {
	public static class BakaTestUnityHook {

		[MenuItem("&Baka/Test &t")]
		public static void RunTests() {
			var testAssembly = Assembly.GetAssembly(typeof(BakaTests));
			var testTypes = testAssembly.DefinedTypes
				.Where(t => t.Name.EndsWith("_Tests"));

			UnityEngine.Debug.Log($"Found {testTypes.Count()} test classes to run");

			foreach (var type in testTypes) {
				try {
					UnityEngine.Debug.Log(BakaTests.RunTests(type));
				} catch (Exception e) {
					UnityEngine.Debug.LogError($"Failed to run tests for type {type}");
					UnityEngine.Debug.LogError(e);
				}
			}
			
		}

	}

}

#endif

namespace BakaTest {
	public static class BakaTests {

		#region Test Framework
		// ~180 lines to get most of Shouldly's functionality.
		#region shouldly-like-extensions
		/// <summary> Generates a short informative string about the type and content of an object </summary>
		/// <param name="obj"> Object to make info about </param>
		/// <returns> Short string with info about the object </returns>
	#if COMP_SERVICES
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
	#endif
		public static string Info(this object obj) {
			if (obj == null) { obj = ActualNull.instance; }
			return string.Format("({0})({1})", obj.GetType().Name, obj);
		}
		public static string[] sep = new string[] { "\n\r", "\r\n", "\n", "\r" };
		public static string CallInfo(this string stackTrace) {
			string[] lines = stackTrace.Split(sep, StringSplitOptions.None);
			string line = lines[lines.Length - 1];
			string method = line.Substring(6, line.IndexOf('(', 6) - 6);
			string fileAndLineStr = line.Substring(line.LastIndexOf('\\') + 1);
			string[] fileAndLine = fileAndLineStr.Split(':');
			string file = fileAndLine[0];
			string lineNumber = fileAndLine[1];

			return string.Format("{0}, in {1}, {2}", method, file, lineNumber);
		}

		public class ActualNull {
			public static readonly ActualNull instance = new ActualNull();
			private ActualNull() { }
			public override string ToString() { return "null"; }
		}

		public static string Fmt(this string s, params object[] args) { return string.Format(s, args); }
		public static string SHOULD_BE_FAILED = "Values\n\t{0}\nand\n\t{1}\nShould have been ==, but were not.";

		// I really wish this shit could be solved properly with a generic method...
		/// <summary> Asserts two string values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this string v1, string v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two bool values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this bool v1, bool v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two decimal values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this decimal v1, decimal v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two double values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this double v1, double v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two float values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this float v1, float v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two char  values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this char v1, char v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two byte values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this byte v1, byte v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two short values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this short v1, short v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two int values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this int v1, int v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two long values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this long v1, long v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }

		/// <summary> Asserts two sbyte values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this sbyte v1, sbyte v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two ushort values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this ushort v1, ushort v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two uint values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this uint v1, uint v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }
		/// <summary> Asserts two ulong values are equal </summary> <param name="v1"> First Value </param> <param name="v2"> Second Value</param>
		public static void ShouldBe(this ulong v1, ulong v2) { if (!(v1 == v2)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(v1, v2)); } }

		/// <summary> Tests two objects, and throws an exception if they are not equal by == in one direction (obj == other) </summary>
		/// <param name="obj"> Object to test </param>
		/// <param name="other"> Object to test against </param>
		public static void ShouldBe(this object obj, object other) {
			if (obj.GetType().IsEnum) { ShouldEqual(obj, other); }
			else if (!(obj == other)) { throw new AssertFailed("ShouldBe", SHOULD_BE_FAILED.Fmt(obj.Info(), other.Info())); }
		}

		/// <summary> 
		/// Checks two objects for equality, using a specific == operator, 
		/// defined in the class of <paramref name="T"/>, between Two <paramref name="T"/>s
		/// </summary>
		/// <typeparam name="T"> First type (expected of <paramref name="obj"/>) </typeparam>
		/// <param name="obj"> First object for comparison. Should be of type <paramref name="T"/> </param>
		/// <param name="other"> Second object for comparison. </param>
		public static void ShouldBe<T>(this object obj, object other) {
			Type type = typeof(T);
			if (!(obj is T)) { throw new AssertFailed("ShouldBe<>", "Value\n\t" + obj.Info() + "\nShould be castable to type\n\t" + type.Name + "\nBut is not."); }
			if (!(other is T)) { throw new AssertFailed("ShouldBe<>", "Value\n\t" + other.Info() + "\nShould be castable to type\n\t" + type.Name + "\nBut is not."); }
			Type[] tarr = new Type[] { type, type };
			MethodInfo op_Equality = type.GetMethod("op_Equality", tarr);
			if (op_Equality == null) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nDoes not have expected custom operator"); }
			if (op_Equality.ReturnType != typeof(bool)) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nHas == operator returning non-bool value."); }
			Type[] argTypes = op_Equality.GetParameters().Select(a => a.ParameterType).ToArray();
			if (argTypes.Length != 2) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nHas == operator having more or less tahn 2 parameters!"); }
			T tobj = (T)obj;
			T tother = (T)other;
			object[] args = new object[] { tobj, tother };
			bool result = (bool)op_Equality.Invoke(null, args);
			if (!result) { throw new AssertFailed("ShouldBe<>", SHOULD_BE_FAILED.Fmt(obj.Info(), other.Info())); }
		}

		/// <summary> 
		/// Checks two objects for equality, using a specific == operator, 
		/// defined in the class of <paramref name="T"/>, 
		/// between a <paramref name="T"/> and a <paramref name="T2"/>
		/// </summary>
		/// <typeparam name="T"> First type (expected of <paramref name="obj"/>) </typeparam>
		/// <typeparam name="T2"> Second type (expected of <paramref name="other"/>) </typeparam>
		/// <param name="obj"> First object for comparison. Should be of type <paramref name="T"/> </param>
		/// <param name="other"> Second object for comparison. Should be of type <paramref name="T2"/> </param>
		public static void ShouldBe<T, T2>(this object obj, object other) {
			Type type = typeof(T);
			Type type2 = typeof(T2);
			if (!(obj is T)) { throw new AssertFailed("ShouldBe<,>", "Value\n\t" + obj.Info() + "\nShould have been type\n\t" + type.Name + "\nBut was not."); }
			if (!(other is T2)) { throw new AssertFailed("ShouldBe<,>", "Value\n\t" + other.Info() + "\nShould have been type\n\t" + type2.Name + " \nBut was not."); }
			Type[] tarr = new Type[] { type, type2 };
			bool tfirst = true;
			MethodInfo op_Equality = type.GetMethod("op_Equality", tarr);
			if (op_Equality == null) { tfirst = false; tarr[0] = type2; tarr[1] = type; op_Equality = type.GetMethod("op_Equality", tarr); }

			if (op_Equality == null) { throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nDoes not have custom == operator defined."); }
			if (op_Equality.ReturnType != typeof(bool)) { throw new AssertFailed("ShouldBe<>", "Type\n\t" + type.Name + "\nHas == operator returning non-bool value."); }

			Type[] argTypes = op_Equality.GetParameters().Select(a => a.ParameterType).ToArray();
			if (argTypes.Length != 2) { throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nHas == operator having more or less tahn 2 parameters!"); }
			if (argTypes[0] != type && argTypes[1] != type) {
				throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nHas == operator without its own type as a parameter");
			}
			if (argTypes[0] != type2 && argTypes[1] != type2) {
				throw new AssertFailed("ShouldBe<,>", "Type\n\t" + type.Name + "\nHas == operator without type\n\t" + type2.Name + "\nas a parameter");
			}


			T tobj = (T)obj;
			T2 t2other = (T2)other;
			object[] args = new object[] { (tfirst ? (object)tobj : (object)t2other), (tfirst ? (object)t2other : (object)tobj) };
			bool result = (bool)op_Equality.Invoke(null, args);
			if (!result) { throw new AssertFailed("ShouldBe<,>", SHOULD_BE_FAILED.Fmt(obj.Info(), other.Info())); }
		}


		/// <summary> Tests two things, and throws an exception if they are equal by != in one direction (obj != other) </summary>
		/// <param name="obj"> Object to test </param>
		/// <param name="other"> Object to test against </param>
		public static void ShouldNotBe(this object obj, object other) {
			if (!(obj != other)) { throw new AssertFailed("ShouldNotBe", "Values\n\t" + obj.Info() + "\nand\n\t" + other.Info() + "\nShould have been !=, but were not."); }
		}

		/// <summary> Tests two things, and throws an exception if they are not equal by Equals in one direction (obj.Equals(other)) </summary>
		/// <param name="obj"> Object to test </param>
		/// <param name="other"> Object to test against </param>
		public static void ShouldEqual(this object obj, object other) {
			if (!obj.Equals(other)) { throw new AssertFailed("ShouldEqual", "Values\n\t" + obj.Info() + "\nand\n\t" + other.Info() + "\nShould have been .Equal(), but were not."); }
		}

		/// <summary> Tests two enumerable collections to make sure their sequences match a predicate that takes values from each sequence </summary>
		/// <typeparam name="T">Generic type </typeparam>
		/// <param name="coll"> Collection to test </param>
		/// <param name="other"> Expected collection to pair with </param>
		/// <param name="predicate"> Predicate to derermine if the test passes. </param>
		public static void ShouldEachMatch<T>(this IEnumerable<T> coll, IEnumerable<T> other, Func<T, T, bool> predicate) {
			var ita = coll.GetEnumerator();
			var itb = other.GetEnumerator();
			int sizeA = 0;
			int sizeB = 0;
			while (ita.MoveNext()) {
				sizeA++;
				if (!itb.MoveNext()) {
					throw new AssertFailed("ShouldBe", $"Collection A was at least size {sizeA}, but Collection B was only size {sizeB}");
				}
				sizeB++;

				var a = ita.Current;
				var b = itb.Current;
				if (!predicate(a, b)) {
					throw new AssertFailed("ShouldBe", $"Values A\n\t{a}\nand B\n\t{b}\nDid not satisfy predicate");
				}

			}
			if (itb.MoveNext()) {
				sizeB++;
				throw new AssertFailed("ShouldBe", $"Collection A was only size {sizeA}, but Collection B was at laest size {sizeB}");
			}
		}



		/// <summary> Tests two things, and throws an exception if they are not equal by Equals in one direction (!obj.Equals(other)) </summary>
		/// <param name="obj"> Object to test </param>
		/// <param name="other"> Object to test against </param>
		public static void ShouldNotEqual(this object obj, object other) {
			if (obj.Equals(other)) { throw new AssertFailed("ShouldNotEqual", "Values\n\t" + obj.Info() + "\nand\n\t" + other.Info() + "\nShould not have been .Equal(), but were."); }
		}

		/// <summary> Tests a boolean expression for truthiness </summary>
		/// <param name="b"> Expression expected to be true </param>
		public static void ShouldBeTrue(this bool b) {
			if (!b) { throw new AssertFailed("ShouldBeTrue", "Expression should have been true, but was false"); }
		}

		/// <summary> Tests a boolean expression for falsity </summary>
		/// <param name="b"> Expression expected to be false </param>
		public static void ShouldBeFalse(this bool b) {
			if (b) { throw new AssertFailed("ShouldBeFalse", "Expression should have been false, but was true"); }
		}

		/// <summary> Tests similarity between arrays. They are considered same if same length and each parallel element equal. </summary>
		/// <typeparam name="T"> Generic type </typeparam>
		/// <param name="a"> First array </param>
		/// <param name="b"> Second (expected) array </param>
		public static void ShouldBeSame<T>(this T[] a, T[] b) {
			if (a == null) { throw new AssertFailed("ShouldBeSame", "Arrays cannot be null- source array was null!"); }
			if (b == null) { throw new AssertFailed("ShouldBeSame", "Arrays cannot be null- expected array was null!"); }
			if (a.Length != b.Length) { throw new AssertFailed("ShouldBeSame", string.Format("Arrays not the same length!\n\tExpected\n\t{1},\n\thad\n\t{0}", a.Length, b.Length)); }
			for (int i = 0; i < a.Length; i++) {
				if (!a[i].Equals(b[i])) {
					throw new AssertFailed("ShouldBeSame", string.Format("Array elements at\n\t{0}\n\tdid not match.\n\tExpected\n\t{2}\n\thad\n\t{1}", i, a[i], b[i]));
				}
			}
		}

		/// <summary> Throws an AssertFailed. Marks a line of code as something that should not be reached. </summary>
		public static void ShouldNotRun() {
			throw new AssertFailed("ShouldNotRun", "Line of code invokign this method should not have been reached.");
		}

		#endregion


		// Too many conditional compiles following...
		// TBD: Find a better way to structure support for multiple platforms
	#if DEBUG
		/// <summary> Output stream to write to, if assigned. </summary>
		internal static TextWriter Out = null;
	#else
		internal static TextWriter Out = null;
	
	#endif
		/// <summary> Debug helper method </summary>
		/// <param name="message"> Message object to output to the assigned outstream </param>
	#if !UNITY // Note: Unity has control over this symbol, so this function shouldn't be marked Conditional in some cases when unity uses DEBUG
		[Conditional("DEBUG")]
	#endif
		public static void Log(object message) {
	#if DEBUG
			if (Out != null) {
				Out.WriteLine(message); 
			}
	#endif
		}

		private static readonly BindingFlags ALL_STATIC = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly BindingFlags ALL = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
		private static readonly Type[] EMPTY_TYPES = new Type[] { };
		private static readonly object[] EMPTY_PARAMS = new object[] { };

		public static List<MethodInfo> GetTestMethods(this Type type) {
			var allMethods = type.GetMethods(ALL_STATIC);
			var nestedTypes = type.GetNestedTypes(ALL);
			List<MethodInfo> tests = new List<MethodInfo>();

			foreach (var method in allMethods) {
				if (method.Name.StartsWith("Test") && method.GetParameters().Count() == 0) { tests.Add(method); }
			}

			foreach (var subtype in nestedTypes) {
				tests.AddRange(subtype.GetTestMethods());
			}

			return tests;
		}

		private static MethodInfo GetCleanupMethod(this Type type) {
			var cleanup = type.GetMethod("Clean", ALL_STATIC);
			
			if (cleanup == null || cleanup.GetParameters().Length != 0) { return null; }

			return cleanup;
		}
	
		private class AssertFailed : Exception {
			internal string type = null;
			internal string description = "No Description Given";
			public AssertFailed() { }
			public AssertFailed(string desc) { description = desc; }
			public AssertFailed(string type, string desc) {
				this.type = type;
				description = desc;
			}
		}
		#endregion

		/// <summary> Runs all of the tests, and returns a string containing information about tests passing/failing. </summary>
		/// <returns> A log of information about the results of the tests. </returns>
		public static string RunTests(params Type[] types) {
			StringBuilder str = "";
			foreach (var type in types) {
				str += RunTests(type);
			}

			return str.ToString();
		}

		public static string RunTests(Type testType) {
			var tests = testType.GetTestMethods();
			var cleanup = testType.GetCleanupMethod();

			Action doCleanup = () => {
				if (cleanup != null) {
					try { cleanup.Invoke(null, EMPTY_PARAMS); } catch (Exception e) {
						UnityEngine.Debug.LogError("Error during cleanup");
						UnityEngine.Debug.LogError(e);
					}
				}
			};
			
			var empty = new object[0];
			MemoryStream logStream = new MemoryStream();
			Encoding encoding = Encoding.ASCII;
			TextWriter logWriter = new StreamWriter(logStream, encoding);

			int success = 0;
			int failure = 0;
			Out = logWriter;
			Log($"Testing for type {testType}");
			Log("Testing Log Follows:");

			foreach (var test in tests) {
				Log("Running (" + test.Name + ")");

				doCleanup();

				try {
					test.Invoke(null, empty);
					Log("\tSuccess!");
					success++;

				} catch (TargetInvocationException e) {
					if (e.InnerException != null) {
						Exception ex = e.InnerException;
						if (ex is AssertFailed) {
							AssertFailed fail = ex as AssertFailed;
							string type = fail.type;
							if (type == null) { type = "Assertion"; }
							Log("\tFailure, " + type + " Failed:\n" + fail.description);
						} else {
							Log("\tFailure, Exception Generated: " + ex.GetType().Name);
							Log("\t\t" + ex.Message);

						}
						Log("\tLocation: " + ex.StackTrace);
						Log("\tInner: " + ex.InnerException);

					}
					failure++;
				} catch (Exception e) {
					Log("Unexpected Exception:\n\t" + e.GetType().Name);
					Log("\tFull Trace: " + e.StackTrace);
					failure++;
				}
				Log("\n");
			}

			logWriter.Flush();
			Out = null;
			StringBuilder strb = new StringBuilder();
			strb.Append(string.Format("Summary: {0} success, {1} failure\n", success, failure));
			strb.Append(encoding.GetString(logStream.ToArray()));

			doCleanup();

			return strb.ToString();
		}



	}
}
