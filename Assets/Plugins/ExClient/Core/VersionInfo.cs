using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex{

	public static class VersionInfo {
		public const int MAJOR = 0;
		public const int MINOR = 0;
		public const int PATCH = 0;
		public static readonly string VERSION = $"v{MAJOR}.{MINOR}.{PATCH}";
	}
}
