using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> All informtation used to generate some data. </summary>
	public struct ItemGenSeed {
		/// <summary> Root ID </summary>
		public Guid root { get; private set; }
		/// <summary> Offset to seed, typically also randomly generated </summary>
		public long offset { get; private set; }
		/// <summary> Scale or Index of item for derived items </summary>
		public double scale { get; private set; }

		/// <summary> Gets the long seed value from root/offset information </summary>
		public long seed {
			get {
				long v = 0;
				byte[] bytes = Unsafe.ToBytes(root);
				byte[] vbytes = Unsafe.ToBytes(v);

				for (int i = 0; i < bytes.Length; i++) {
					vbytes[i % StructInfo<long>.size] ^= bytes[(37 * i) % StructInfo<Guid>.size];
				}
				v = Unsafe.FromBytes<long>(vbytes);
				v += offset * 31337;
				return v * (v < 0 ? -1 : 1);
			}
		}

		/// <summary> Constructor with optional offset/index parameters </summary>
		/// <param name="root"> Root GUID to use for seed </param>
		/// <param name="offset"> Offset from root </param>
		/// <param name="index"> Index of seed </param>
		public ItemGenSeed(Guid root, long? offset = null, double? scale = null) {
			this.root = root;
			this.offset = offset ?? 0;
			this.scale = scale ?? 0;
		}

		/// <summary> Returns a modified seed with +1 to the <see cref="index"/> field </summary>
		/// <returns> Returns a modified seed with +1 to the <see cref="index"/> field </returns>
		public ItemGenSeed Next() {
			ItemGenSeed next = this;
			next.scale += 1.0;
			return next;
		}

		/// <summary> Returns a modified seed with the given <see cref="index"/> </summary>
		/// <param name="index"> Index value to use </param>
		/// <returns> Returns a modified seed with the given <see cref="index"/> </returns>
		public ItemGenSeed Scale(double scale) {
			ItemGenSeed next = this;
			next.scale = scale;
			return next;
		}

		/// <summary> Returns a modified seed with the given <see cref="offset"/> </summary>
		/// <param name="offset"> Index value to use </param>
		/// <returns> Returns a modified seed with the given <see cref="offset"/> </returns>
		public ItemGenSeed Offset(long offset) {
			ItemGenSeed next = this;
			next.offset = offset;
			return next;
		}
	}

	/// <summary> Procedural data generator </summary>
	public class Generator {

		/// <summary> Ruleset to use to generate data </summary>
		private JsonObject ruleset;

		/// <summary> Dictionary holding extra stuff </summary>
		private Dictionary<string, Action<JsonObject>> extras;

		/// <summary> Used to hold the state of a generation process </summary>
		private class State {
			/// <summary> Seed used to begin generation </summary>
			public ItemGenSeed igSeed;
			/// <summary> Current random state </summary>
			public Random random;
			/// <summary> Path to the current rule </summary>
			public List<string> path;
			/// <summary> Resulting generated data </summary>
			public JsonObject result;
			/// <summary> Last history item </summary>
			public JsonObject lastHistory { get { return genHistory.Get<JsonObject>(genHistory.Count-1); } }
			/// <summary> Entire generation history, array of JsonObjects </summary>
			public JsonArray genHistory { get { return result.Get<JsonArray>("genHistory"); } }
			/// <summary> scale property from seed </summary>
			public double scale { get { return igSeed.scale; } }
			/// <summary> offset property from seed </summary>
			public long offset { get { return igSeed.offset; } }
			/// <summary> root property from seed </summary>
			public Guid root { get { return igSeed.root; } }

			public State(ItemGenSeed igSeed) {
				this.igSeed = igSeed;
				// Resulting creation + history
				result = new JsonObject(
					"name", "",
					"genId", "",
					"root", igSeed.root.ToString(),
					"offset", igSeed.offset,
					"scale", igSeed.scale,
					"genHistory", new JsonArray()
				);
				
				path = new List<string>();
				
				long seed = igSeed.seed;
				long hi = (seed >> 32) & 0x00000000FFFFFFFF;
				long low = seed & 0x00000000FFFFFFFF;

				int mix = (int)(low ^ hi);
				random = new Random(mix < 0 ? -mix : mix);
			}

			/// <summary> Chooses one key from an object, using paired numbers as weights to select. 
			/// Consumes one RNG tick. </summary>
			/// <param name="weights"> JsonObject to iterate over. Only numeric values are considered. </param>
			/// <returns> JsonString of key selected. </returns>
			public JsonString WeightedChoose(JsonObject weights) {
				float total = 0;
				JsonString last = "";
				foreach (var pair in weights) {
					if (pair.Value.isNumber) {
						total += pair.Value;
						last = pair.Key;
					}
				}

				float choose = (float)(random.NextDouble()) * total;
				float check = 0;
				foreach (var pair in weights) {
					if (pair.Value.isNumber) {
						check += pair.Value;
						if (check > choose) { return pair.Key; }
					}
				}

				return last;
			}
			
			/// <summary> Chooses an item from a JsonArray. Consumes one RNG tick. </summary>
			/// <param name="array"> Array to choose from. </param>
			/// <returns> Selected JsonValue </returns>
			public JsonValue Choose(JsonArray array) {
				int i = random.Next(array.Count);
				return array[i];
			}

			// Don't know if I actually need these or not.
			/*
			/// <summary> Reduces a JsonValue into an int, 
			/// recursively randomly selecting keys from objects, or treating an array as a range if needed.   </summary>
			/// <param name="value"> Value to reduce to a int </param>
			/// <returns> Value reduced to a int </returns>
			public int ReduceToInt(JsonValue value) {
				if (value.isNumber) { return value.intVal; }
				if (value.isObject) { return ReduceToInt(WeightedChoose(value as JsonObject)); }
				if (value.isArray) { 
					JsonArray arr = value as JsonArray;
					int a = arr[0].intVal;
					int b = arr[1].intVal;
					return a + (int)((b-a) * NextRand());
				}
				return 0;
			}

			/// <summary> Reduces a JsonValue into a double, 
			/// recursively randomly selecting keys from objects, or treating an array as a range if needed.  </summary>
			/// <param name="value"> Value to reduce to a double </param>
			/// <returns> Value reduced to a double </returns>
			public double ReduceToDouble(JsonValue value) {
				if (value.isNumber) { return value.doubleVal; }
				if (value.isObject) { return ReduceToDouble(WeightedChoose(value as JsonObject)); }
				if (value.isArray) {
					JsonArray arr = value as JsonArray;
					double a = arr[0].intVal;
					double b = arr[1].intVal;
					return a + ((b - a) * NextRand());
				}
				return 0;
			}


			/// <summary> Reduces a JsonValue into a float, 
			/// recursively randomly selecting keys from objects, or treating an array as a range if needed. </summary>
			/// <param name="value"> Value to reduce to a float </param>
			/// <returns> Value reduced to a float </returns>
			public float ReduceToFloat(JsonValue value) {
				if (value.isNumber) { return value.floatVal; }
				if (value.isObject) { return ReduceToFloat(WeightedChoose(value as JsonObject)); }
				if (value.isArray) {
					JsonArray arr = value as JsonArray;
					float a = arr[0].intVal;
					float b = arr[1].intVal;
					return a + (float)((b - a) * NextRand());
				}
				return 0;
			}
			//*/

			/// <summary> Reduces a JsonValue into a string, 
			/// recursively randomly selecting keys/indicies from objects/arrays if needed.  </summary>
			/// <param name="value"> Value to reduce to a string </param>
			/// <returns> Value reduced to a string </returns>
			public string ReduceToString(JsonValue value) {
				if (value == null) { return ""; }
				if (value.isString || value.isNumber || value.isBool) { return value.stringVal; }
				if (value.isObject) { return ReduceToString(WeightedChoose(value as JsonObject)); }
				if (value.isArray) { return ReduceToString(Choose(value as JsonArray)); }
				return "";
			}

			

			/// <summary> Tells the state to move 'down' into the next rule. </summary>
			/// <param name="newPath"> Next step along the rule path </param>
			public void PushHistory(string newPath) {
				path.Add(newPath);
				genHistory.Add(new JsonObject("path", string.Join(".", path), "rolls", new JsonObject()));
			}
			/// <summary> Tells the state to move 'up' into the previous rule. </summary>
			public void PopHistory() {
				if (path.Count > 0) {
					path.RemoveAt(path.Count - 1);
				}
			}

			/// <summary> Next equally distributed double value </summary>
			/// <returns> An equally distributed double val [0, 1) </returns>
			public double NextRand() {
				return random.NextDouble();
			}
			/// <summary> Next normally distributed double value </summary>
			/// <returns> A normally distributed double val [0, 1) </returns>
			public double NextNorm() {
				return (random.NextDouble() + random.NextDouble() + random.NextDouble()) / 3.0;
			}
		}

		public Generator(JsonObject ruleset) {
			this.ruleset = ruleset;
			extras = new Dictionary<string, Action<JsonObject>>();
			extras["capitalize"] = (obj) => {
				var name = obj.Pull("name", "");
				if (name.Length > 0) {
					name = char.ToUpper(name[0]) + name.Substring(1).ToLower();
				}
				obj["name"] = name;
			};
		}

		/// <summary> Registers an extra for this generator. </summary>
		/// <param name="name"> Name of extra to register </param>
		/// <param name="extra"> Extra to register. </param>
		public void AddExtra(string name, Action<JsonObject> extra) {
			extras[name] = extra;
		}

		/// <summary> Generate some data, starting at a given rule, with a given seed. </summary>
		/// <param name="startingRule"> Name of rule to search for and begin at </param>
		/// <param name="igSeed"> Seed value used to generate data </param>
		/// <returns> Generated data + history. </returns>
		public JsonObject Generate(string startingRule, ItemGenSeed igSeed) {
			State state = new State(igSeed);
			state.result["startingRule"] = startingRule;
			state.result["genType"] = ruleset.Get<string>("type");
			state.result["genId"] = startingRule;

			JsonObject initializer = ruleset.Pull<JsonObject>("initialize");
			if (initializer != null) {
				state.result.SetRecursively(initializer);
			}

			long lseed = igSeed.seed;
			double scale = igSeed.scale;
			

			// The rule is to add the empty history for the next rule before calling Apply
			// This allows Apply to figure out exactly where in the chain it is.
			state.PushHistory(startingRule);
			Apply(state, startingRule, ruleset);
			state.PopHistory();
			
			return state.result;
		}
		
		/// <summary> Primary workhorse, applies a single rule to an object. </summary>
		/// <param name="state"> Current generation state to modify </param>
		/// <param name="rule"> Rule to apply </param>
		/// <param name="history"> History to reuse or create. </param>
		private void ApplyRule(State state, JsonObject rule, JsonObject history) {

			Console.WriteLine("Applying rule " + state.lastHistory["path"].stringVal);

			var rolls = history.Get<JsonObject>("rolls");
			bool firstTime = rolls.Count == 0;
			// Record a roll so we don't try to recurse when replaying history, even for rules with no rolls.
			if (firstTime) { rolls["didIt_"] = true; }

			if (firstTime) {
				// Apply fixed-function stuff
				if (rule.Has("id")) { // Always string 
					var id = state.result.Pull("genId", "");
					state.result["genId"] = id + rule.Get<string>("id");
				}
				
				if (rule.Has("prefix")) {
					string prefix = state.ReduceToString(rule["prefix"]);
					state.result["name"] = prefix + state.result.Get<string>("name");
				}

				if (rule.Has("suffix")) {
					string suffix = state.ReduceToString(rule["suffix"]);
					state.result["name"] = state.result.Get<string>("name") + suffix;
				}

				if (rule.Has("set")) {
					JsonObject setter = rule.Get<JsonObject>("set");
					foreach (var pair in setter) {
						var key = pair.Key;
						var val = pair.Value;
						state.result[key] = state.ReduceToString(val);
					}
				}
				if (rule.Has("mult")) {
					JsonObject multer = rule.Get<JsonObject>("mult");
					foreach (var pair in multer) {
						var key = pair.Key;
						var val = pair.Value;
						if (val.isNumber && state.result[key].isNumber) {
							state.result[key] *= val.doubleVal;
						}
					}
				}


				// Todo: Maybe choose a better name for this too?
				// Conditionally applied rules
				if (rule.Has("check")) {
					JsonObject checker = rule.Get<JsonObject>("check");
					foreach (var pair in checker) {
						var key = pair.Key;
						var val = pair.Value as JsonObject;
						if (val == null) { continue; }
						var read = state.result[key];

						if (read.isString) {
							if (val.Has(read.stringVal)) {
								var nested = val[read];
								if (nested.isObject) {
									// Directly apply nested rule
									// Adds '.'s around such that the rule traversal can still locate the applied rule object.
									state.PushHistory($"check.{key.stringVal}.{read.stringVal}");
									ApplyRule(state, nested as JsonObject, state.lastHistory);
									state.PopHistory();
								}
							}
						} else if (read.isNumber) {
							// Todo: Number range matching
						}
					}
				}
				
				// TODO: Think of a better name.
				if (rule.Has("extras")) {
					JsonArray extras = rule.Get<JsonArray>("extras");
					foreach (var extra in extras) {
						if (extra.isString && this.extras.ContainsKey(extra.stringVal)) {
							try {
								this.extras[extra](state.result);
							} catch (Exception e) {
								Log.Warning($"Error during extra {{{extra.stringVal}}} ", e);
							}
						}
					}
				}
			}
			
			// Loop through all key/value pairs present.
			// 
			foreach (var pair in rule) {
				var key = pair.Key.stringVal;
				if (statGroupTable.ContainsKey(key)) {
					ApplyStatGroup(state, rule, key, rolls);
				}
			}

			// Only recursively apply rules the first time
			// If we are replaying history, the list of previously 
			// applied rule paths is iterated externally.
			if (firstTime) {
				if (rule.ContainsKey("apply")) {
					if (rule["apply"].isString) {
						state.PushHistory("");
					}
					// We recursing, grab the callstack!
					Apply(state, rule["apply"], rule);
					if (rule["apply"].isString) {
						state.PopHistory();
					}
				}
			}
		}


		/// <summary> Apply a stat group to the result </summary>
		/// <param name="state"> Current generator state </param>
		/// <param name="rule"> Rule object </param>
		/// <param name="group"> Name of stat group to apply </param>
		/// <param name="rolls"> History of rolls to use, or record to. </param>
		private void ApplyStatGroup(State state, JsonObject rule, string group, JsonObject rolls) {
			StatTableEntry entry = statGroupTable[group];
			JsonObject stats = rule.Get<JsonObject>(group);

			if (stats != null) {

				if (!rolls.Has(group)) { rolls[group] = new JsonObject(); }
				var rollGroup = rolls.Get<JsonObject>(group);

				foreach (var pair in stats) {
					string key = pair.Key;
					var val = pair.Value;

					var setCheck = FindRule(key);
					if (setCheck != null && setCheck.isObject) {
						foreach (var pair2 in setCheck) {
							var stat = pair2.Key;
							ApplyStat(state, stat, val, entry, rollGroup);
						}

					} else {
						ApplyStat(state, key, val, entry, rollGroup);
					}


				}
			}

		}

		/// <summary> Applies a single stat to the result. </summary>
		/// <param name="state"> Current state object </param>
		/// <param name="stat"> Stat to apply </param>
		/// <param name="statValue"> Value to apply to stat </param>
		/// <param name="randomizer"> Function to use to generate random values </param>
		/// <param name="scaler"> Function to use to scale appleied values </param>
		/// <param name="rollGroup"> History of rolls to use, or record to. </param>
		private void ApplyStat(State state, string stat, JsonValue statValue, StatTableEntry entry, JsonObject rollGroup) {
			if (rollGroup.Has(stat)) {
				// Roll already exists? Replay calculation only.
				
				if (statValue.isNumber) {
					state.result[stat] = entry.applier(state.result.Get<double>(stat), entry.scaler(state, statValue * rollGroup.Get<double>(stat)));
				} else if (statValue.isArray) {
					var a = statValue[0].doubleVal;
					var b = statValue[1].doubleVal;
					state.result[stat] = entry.applier(state.result.Get<double>(stat), entry.scaler(state, a + (b-a) * rollGroup.Get<double>(stat)));
				}

			} else {
				// Roll does not exist? Roll and calculate.
				var roll = entry.randomizer(state);
				if (statValue.isNumber) {
					state.result[stat] = entry.applier(state.result.Get<double>(stat), entry.scaler(state, statValue * roll));
				} else if (statValue.isArray) {
					var a = statValue[0].doubleVal;
					var b = statValue[1].doubleVal;
					state.result[stat] = entry.applier(state.result.Get<double>(stat), entry.scaler(state, a + (b-a) * roll));
				}

				rollGroup[stat] = roll;
			}
		}

		/// <summary><para>Recursive funhouse of applying rules different ways depending on how they are described. 
		/// This figures out how a rule should be applied. Rules are described in data as <paramref name="thing"/>. </para>
		/// <para>Applying an array causes every element of the array to be applied.</para>
		/// <para>Applying an object causes a 'key' of the object to be selected using WeightedChoose, then the key is applied.</para>
		/// <para>Applying a string causes us to search for the next rule named by the string, in the current <paramref name="rule"/>,
		///	or if it can't be found there, in the <see cref="ruleset"/>. </para>
		///	<para>Regardless of what is selected, the change in rules is recorded in the <see cref="State.genHistory"/></para></summary>
		/// <param name="state"> Current generator state </param>
		/// <param name="thing"> The thing we are trying to apply </param>
		/// <param name="rule"> The rule we are currently in (so we can find nested rules) </param>
		private void Apply(State state, JsonValue thing, JsonObject rule) {
			string path = state.lastHistory["path"].stringVal;

			// Rules eventually are told to us by strings describing them.
			if (thing.isString) {
				Console.WriteLine($"Applying String rule at {path} => {thing.stringVal}");
				var nextRule = FindRule(rule, thing.stringVal);
				if (nextRule is JsonObject) {
					ApplyRule(state, nextRule as JsonObject, state.lastHistory);
				}
			}

			// May have arrays to describe a sequence of rules to apply
			if (thing.isArray) {
				Console.WriteLine($"Applying Array rule at {path} => {thing.ToString()}");
				foreach (var val in (thing as JsonArray)) {
					if (val.isString) {
						// If we're at a string, add it then apply it, since the next Apply will invoke a rule.
						state.PushHistory(val.stringVal);
					}
					Apply(state, val, rule);
					if (val.isString) {
						state.PopHistory();
					}
				}
			}
			// If we're in an object, we need to make a decision of what name to apply,
			// as well as other things.
			if (thing.isObject) {
				if (thing.ContainsKey("repeat") && thing.ContainsKey("rule")) {
					// Meta Object: Contains instruction to repeat something multiple times...
					RepeatedApply(state, thing, rule, path);

				// todo later: support other meta-applications with objects describing them...
				} else {
					string chosen = state.WeightedChoose(thing as JsonObject);
					Console.WriteLine($"Applying Weighted Choice rule at {path} => {chosen}");
					state.PushHistory(chosen);
					Apply(state, chosen, rule);
					state.PopHistory();
				}
			}
		}

		/// <summary> Helper to minimize the bloat in <see cref="Apply"/>. 
		/// Handles repeating application of a rule as described in a meta-apply object. </summary>
		/// <param name="state"> Current generator state </param>
		/// <param name="thing"> Thing to apply </param>
		/// <param name="rule"> Current rule object </param>
		/// <param name="path"> Current path </param>
		private void RepeatedApply(State state, JsonValue thing, JsonObject rule, string path) {
			JsonObject info = thing as JsonObject;
			JsonValue reps = info["repeat"];
			string ruleName = state.ReduceToString(info["rule"]);
			Console.WriteLine($"Applying Repeat rule at {path} => {ruleName} x {reps.ToString()}");
			// Try to reduce reps into a number...
			// if we want to do something an exact number of times, reps will just be a number.
			// if we want a simple randInt range, it will be an array
			if (reps.isArray) {
				int low = reps[0].intVal;
				int hi = reps[1].intVal;
				reps = low + state.random.Next(hi - low);
			}

			if (reps.isNumber) {
				Console.WriteLine($"Doing Repeat rule at {path} => {ruleName} x {reps.ToString()}");
				int rep = reps.intVal;
				for (int i = 0; i < rep; i++) {
					state.PushHistory(ruleName);
					Apply(state, ruleName, rule);
					state.PopHistory();
				}
			}
		}


		/// <summary> Helper method to locate nested information. </summary>
		/// <param name="context"> Object to start search for </param>
		/// <param name="path"> Path to search, starting within object, then within the whole ruleset. </param>
		/// <returns> Rule object in ruleset at path </returns>
		private JsonValue FindRule(JsonObject context, string path) {
			if (path == null || path == "") { return null; }
			// If we have the path within our context, return it
			if (context.ContainsKey(path)) { return context[path]; }
			// Otherwise, if the root has it, jump there.
			if (ruleset.ContainsKey(path)) { return ruleset[path]; }
			// Otherwise, it may be an absolute path, so try to find it that way.

			// This will allow us to track what invoked a rule, even if the rule is not nested within.
			// Example paths of rules applied to something: 
			/* Mineral
			 * Mineral.Atomic
			 * Mineral.Atomic.Name
			 * Mineral.Atomic.Name.Mid
			 * Mineral.Atomic.Name.Mid // applied second time
			 * Mineral.Atomic.Name.Mid // applied third time
			 * Mineral.Atomic.Counts
			 * Mineral.Atomic.Counts.Duo
			 * Mineral.Atomic.AlkalaiMetal */
			// We want to find any rules, regardless of if they are nested or not
			// And we don't care if they are actually nested or not (if they are, it does matter to the actual item )
			// For example, Mineral.Compound.Name may be a rule nested within 'Compound',
			// in that case it would be a different rule than the 'Mineral.Atomic.Name' rule.

			return FindRule(path);
		}
		/// <summary> Helper method to locate nested information. </summary>
		/// <param name="absolutePath"> String describing absolute path to search for </param>
		/// <returns> Rule object in ruleset at path </returns>
		private JsonObject FindRule(string absolutePath) {
			if (absolutePath == null || absolutePath == "") { return null; }
			string[] splits = absolutePath.Split('.');

			JsonObject obj = ruleset;

			foreach (var step in splits) {
				// Try to find object under current place
				if (obj != null) {
					obj = obj.Pull<JsonObject>(step, null);
				}
				// If we can't, reset back to the top 
				// (similar to what we do in the other FindRule)
				// This lets us record and replay pathing inside of complex rule objects
				if (obj == null) {
					obj = ruleset.Pull<JsonObject>(step, null);
				}
			}

			return obj;
		}



		/// <summary> Pre-baked stat group table with all variations of stat applier names </summary>
		private static readonly Dictionary<string, StatTableEntry> statGroupTable = BuildStatTable();
		/// <summary> Function that bakes stat group table </summary>
		/// <returns> Baked stat group table </returns>
		private static Dictionary<string, StatTableEntry> BuildStatTable() {
			Dictionary<string, StatTableEntry> table = new Dictionary<string, StatTableEntry>();

			// Randomizers 
			// stat (1.0 always)
			double stat(State state) { return 1.0; }
			// rand [0, 1)
			double rand(State state) { return state.NextRand(); }
			// normal (approximate) in [0, 1), with .5 avg
			double norm(State state) { return state.NextNorm(); }

			// Scalers
			// normal scaling (v*scale)
			double mscale(State state, double v) { return v * state.scale; }
			// divisor scaling (v/scale)
			double dscale(State state, double v) { return v / state.scale; }
			// power scaling (v^scale)
			double pscale(State state, double v) { return Math.Pow(v, state.scale); }
			// fixed scaling (v)
			double f(State state, double v) { return v; }
			// fixed sqrt scaling (sqrt(v))
			double sqrt(State state, double v) { return Math.Sqrt(v); }

			// Add
			double add(double x, double y) { return (x + y); }
			// Subtract
			double sub(double x, double y) { return (x - y); }
			// Multiply
			double mult(double x, double y) { return (x * y); }
			// Divide
			double div(double x, double y) { return (x / y); }
			// power
			double pow(double x, double y) { return Math.Pow(x, y); }
			// combine Ratios (eg, (.5,.5) => .75, (.6, .5) => .8
			double ratio(double x, double y) { return OM(OM(x) * OM(y)); }
			double Clamp01(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
			double OM(double v) { return 1.0 - Clamp01(v); } // One Minus 

			StatTableEntry baseEntry = new StatTableEntry(stat, mscale, add);
			table[nameof(stat)] = new StatTableEntry(baseEntry);
			table[nameof(rand)] = new StatTableEntry(baseEntry); table[nameof(rand)].randomizer = rand;
			table[nameof(norm)] = new StatTableEntry(baseEntry); table[nameof(norm)].randomizer = norm;

			Func<State, double, double>[] scalers = new Func<State, double, double>[] { mscale, dscale, pscale, f, sqrt };
			string[] scalerNs = new string[] { nameof(mscale), nameof(dscale), nameof(pscale), nameof(f), nameof(sqrt), };

			Func<double, double, double>[] appliers = new Func<double, double, double>[] { add, sub, mult, div, ratio, pow };
			string[] applierNs = new string[] { nameof(add), nameof(sub), nameof(mult), nameof(div), nameof(ratio), nameof(pow) };

			var keys = new List<string>();
			keys.Clear(); keys.AddRange(table.Keys);
			foreach (var key in keys) {
				var entry = table[key];
				for (int i = 0; i < scalers.Length; i++) {
					string dkey = scalerNs[i] + key;
					var copy = new StatTableEntry(entry);
					copy.scaler = scalers[i];
					table[dkey] = copy;
				}
			}

			keys.Clear(); keys.AddRange(table.Keys);
			foreach (var key in keys) {
				var entry = table[key];
				for (int i = 0; i < appliers.Length; i++) {
					string dkey = applierNs[i] + key;
					var copy = new StatTableEntry(entry);
					copy.applier = appliers[i];
					table[dkey] = copy;
				}
			}

			return table;
		}

		/// <summary> Class holding information about how to calculate and apply stat groups </summary>
		private class StatTableEntry {
			/// <summary> Function to roll a dice to vary stats on generated things </summary>
			public Func<State, double> randomizer;
			/// <summary> Function to use to apply scaling to output </summary>
			public Func<State, double, double> scaler;
			/// <summary> Function to use accumulate new value with existing value </summary>
			public Func<double, double, double> applier;
			public StatTableEntry(Func<State, double> randomizer, Func<State, double, double> scaler, Func<double, double, double> applier) {
				this.randomizer = randomizer;
				this.scaler = scaler;
				this.applier = applier;
			}
			public StatTableEntry(StatTableEntry other) {
				this.randomizer = other.randomizer;
				this.scaler = other.scaler;
				this.applier = other.applier;
			}
		}
	}
	
}
