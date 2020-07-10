using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex.Utils {
	public delegate string Command(string[] splits);
	public class Cmdr {
		public static string Noop(string[] args) { return ""; }

		private Dictionary<string, Command> commands;
		private List<string> history;
		private int _MaxHistory;

		private int cursor = 0;

		public int MaxHistory {
			get {
				return _MaxHistory;
			}
			set {
				if (value > 0) {
					_MaxHistory = value;
					if (_MaxHistory > 0 && history.Count > _MaxHistory) {
						history.RemoveRange(0, history.Count - _MaxHistory);
					}
				}
			}
		}

		public Cmdr() {
			commands = new Dictionary<string, Command>();
			history = new List<string>();
		}

		private static readonly char[] DELIMS = new char[] { ' ' };
		public string Execute(string command) {
			string[] splits = command.Split(DELIMS);

			string cmdName = splits[0];
			history.Add(command);
			cursor = history.Count;

			return this[cmdName]?.Invoke(splits);
		}

		public void ClearHistory() {
			history.Clear();
		}

		private void ClampCursor() {
			// Intentionally allowed to become history.Count
			// This happens when a command is submitted, and also used to 
			// allow the user to clear the command by hitting down on their last command.
			if (cursor < 0) { cursor = 0; }
			if (cursor > history.Count) { cursor = history.Count; }
		}

		public string NextCommand() {
			cursor++;
			ClampCursor();

			if (cursor >= 0 && cursor < history.Count) {
				return history[cursor];
			}
			return "";
		}

		public string PreviousCommand() {
			cursor--;
			ClampCursor();
			if (cursor >= 0 && cursor < history.Count) {
				return history[cursor];
			}
			return "";
		}

		public Command this[string c] {
			get { 
				if (commands.ContainsKey(c)) {
					return commands[c];
				}	
				return Noop;
			}
			set {
				if (value != null) {
					commands[c] = value;
				} else if (commands.ContainsKey(c)) {
					commands.Remove(c);
				}
			}
		}


	}
}
