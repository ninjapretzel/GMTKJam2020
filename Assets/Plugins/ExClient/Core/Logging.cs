using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Enum of log levels. </summary>
	public enum LogLevel {
		/// <summary> Disables all logging from the BakaNet library, minus exceptions. </summary>
		Error = 0,
		/// <summary> Logs only important information, such as when servers/clients start and stop. </summary>
		Warning = 1,
		/// <summary> Logs most information, such as when tasks start and stop.  </summary>
		Info = 2,
		/// <summary> Logs more information, such as network messages </summary>
		Debug = 3,
		/// <summary> Logs ALL information, including heartbeats. </summary>
		Verbose = 4,
	}

	
	public delegate void Logger(string tag, string msg);


	public static class Log {

		public static readonly string[] LEVEL_CODES = { "\\r", "\\y", "\\w", "\\h", "\\d" };
		public static string ColorCode(LogLevel level) { return (LEVEL_CODES[(int)level]); }
		/// <summary> Path to use to filter file paths </summary>
		public static string ignorePath = null;
		/// <summary> Path to insert infront of filtered paths </summary>
		public static string fromPath = null;

		/// <summary> True to insert backslash color codes. </summary>
		public static bool colorCodes = true;

		/// <summary> Current active log level </summary>
		public static LogLevel logLevel = LogLevel.Verbose;
		/// <summary> Log handler to use to print logs </summary>
		public static Logger logHandler;

		/// <summary> Logs a message using the Verbose LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Verbose(object obj, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Verbose, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Verbose LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Verbose(object obj, Exception ex, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Verbose, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Debug LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Debug(object obj, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Debug, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Debug LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Debug(object obj, Exception ex, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Debug, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Info LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Info(object obj, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Info, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Info LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Info(object obj, Exception ex, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Info, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Warning LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Warning(object obj, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Warning, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Warning LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Warning(object obj, Exception ex, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Warning, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Logs a message using the Error LogLevel. </summary>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Error(object obj, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(null, obj, LogLevel.Error, tag, callerName, callerPath, callerLine);
		}
		/// <summary> Logs an excpetion and message using the Error LogLevel. </summary>
		/// <param name="ex"> Exception to log. </param>
		/// <param name="obj"> Message to log. </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Error(object obj, Exception ex, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {
			log(ex, obj, LogLevel.Error, tag, callerName, callerPath, callerLine);
		}

		/// <summary> Primary workhorse logging method with all options. </summary>
		/// <param name="ex"> Exception to log </param>
		/// <param name="obj"> Message to log </param>
		/// <param name="level"> Minimum log level to use </param>
		/// <param name="tag"> Tag to log with </param>
		/// <param name="callerName">Name of calling method </param>
		/// <param name="callerPath">File of calling method </param>
		/// <param name="callerLine">Line number of calling method </param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void log(Exception ex, object obj, LogLevel level = LogLevel.Info, string tag = "Baka",
				[CallerMemberName] string callerName = "[NO METHOD]",
				[CallerFilePath] string callerPath = "[NO PATH]",
				[CallerLineNumber] int callerLine = -1) {

			if (logLevel >= level) {
				if (obj == null) { obj = "[null]"; }
				string callerInfo = CallerInfo(callerName, callerPath, callerLine, level);
				string message = (colorCodes ? ColorCode(level) : "") + obj.ToString() 
					+ (ex != null ? $"\n{ex.InfoString()}" : "")
					+ callerInfo;

				logHandler?.Invoke(tag, message);
			}
		}
		
		/// <summary> Little helper method to consistantly format caller information </summary>
		/// <param name="callerName"> Name of method </param>
		/// <param name="callerPath"> Path of file method is contained in </param>
		/// <param name="callerLine"> Line in file where log is called. </param>
		/// <returns>Formatted caller info</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] // Gotta go fast. 
		public static string CallerInfo(string callerName, string callerPath, int callerLine, LogLevel level) {
			string path = (fromPath != null ? fromPath : "")
				+ (ignorePath != null && callerPath.Contains(ignorePath)
					? callerPath.Substring(callerPath.IndexOf(ignorePath) + ignorePath.Length)
					: callerPath);
			return (colorCodes ? "\\d" : "")
				+ $"\n{level.ToString()[0]}: [{DateTime.UtcNow.UnixTimestamp()}] by "
				+ ForwardSlashPath(path)
				+ $" at {callerLine} in {callerName}()";
		}
		private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long UnixTimestamp(this DateTime date) {
			TimeSpan diff = date.ToUniversalTime().Subtract(epoch);
			return (long)diff.TotalMilliseconds;
		}
		/// <summary> Convert a file or folder path to only contain forward slashes '/' instead of backslashes '\'. </summary>
		/// <param name="path"> Path to convert </param>
		/// <returns> <paramref name="path"/> with all '\' characters replaced with '/' </returns>
		public static string ForwardSlashPath(string path) {
			string s = path.Replace('\\', '/');
			return s;
		}

		/// <summary> Converts backslashes to forward slashes in the info string. </summary>
		/// <param name="e"> Exception to print </param>
		/// <returns> String containing info about an exception, and all of its inner exceptions, with forward slashes in the stack trace paths. </returns>
		public static string FInfoString(this Exception e) {
			return ForwardSlashPath(InfoString(e));
		}
		/// <summary> Converts backslashes to forward slashes in the mini info string. </summary>
		/// <param name="e"> Exception to print </param>
		/// <returns> String containing info about an exception, with forward slashes in the stack trace paths. </returns>
		public static string FMiniInfoString(this Exception e) {
			return ForwardSlashPath(MiniInfoString(e));
		}

		/// <summary> Constructs a string with information about an exception, and all of its inner exceptions. </summary>
		/// <param name="e"> Exception to print. </param>
		/// <returns> String containing info about an exception, and all of its inner exceptions. </returns>
		public static string InfoString(this Exception e) {
			StringBuilder str = "\nException Info: " + e.MiniInfoString();
			str += "\n\tMessage: " + e.Message;
			Exception ex = e.InnerException;

			while (ex != null) {
				str = str + "\n\tInner Exception: " + ex.MiniInfoString();
				ex = ex.InnerException;
			}


			return str;
		}

		/// <summary> Constructs a string with information about an exception. </summary>
		/// <param name="e"> Exception to print </param>
		/// <returns> String containing exception type, message, and stack trace. </returns>
		public static string MiniInfoString(this Exception e) {
			StringBuilder str = e.GetType().ToString();
			str = str + "\n\tMessage: " + e.Message;
			str = str + "\nStack Trace: " + e.StackTrace;
			return ForwardSlashPath(str);
		}

	}
}
