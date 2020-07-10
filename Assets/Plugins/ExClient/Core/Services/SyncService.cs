using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ex.Utils;

namespace Ex {
	
	/// <summary> Generalized data synchronization service </summary>
	public class SyncService : Service {

		/// <summary> Contexts created for this sync service </summary>
		private ConcurrentDictionary<string, SyncData> contexts;
		/// <summary> Retrieve a SyncData context by name. Creates a context if one does not already exist. </summary>
		/// <param name="name"> Name of context to retrieve. </param>
		/// <returns> SyncData context with the given name. </returns>
		public SyncData Context(string name) {
			if (!contexts.ContainsKey(name)) {
				var ctx = contexts[name] = new SyncData(this, name);
				ctx.OnEnable();
			}
			return contexts[name];
		}

		public bool CheckDirty() {
			foreach (var pair in contexts) {
				pair.Value.CheckDirty();
			}

			return enabled;
		}
		
		Thread updater;
		public override void OnEnable() {
			contexts = new ConcurrentDictionary<string, SyncData>();
			
			if (server.isMaster) {
				updater = server.CreateUpdateThread(CheckDirty, 100);
			}
		}
		public override void OnConnected(Client client) {
			if (client.isMaster) {
				foreach (var pair in contexts) {
					pair.Value.DoSubscribe(client, pair.Value.defaultSubs);
				}
			}
		}

		public override void OnDisable() {
			// May be neccessary 
			//updater?.Join(); 
		}

		/// <summary> RPC, Client->Server, subscribes client to given sets of data. </summary>
		/// <param name="msg"> RPC Message info. </param>
		public void Subscribe(RPCMessage msg) {
			string target = msg[0];
			SyncData context = Context(target);

			if (server.isMaster) {

				string[] args = new string[msg.numArgs - 1]; // Skip name of context in first arg
				for (int i = 1; i < msg.numArgs; i++) { args[i - 1] = msg[i]; }

				context.DoSubscribe(msg.sender, args);
			}
		}

		public void Unsubscribe(RPCMessage msg) {
			string target = msg[0];
			SyncData context = Context(target);

			if (server.isMaster) {

				string[] args = new string[msg.numArgs - 1];
				for (int i = 1; i < msg.numArgs; i++) { args[i - 1] = msg[i]; } // Skip the first arg, as it is the context. 

				context.DoUnsubscribe(msg.sender, args);
			}
		}


		/// <summary> RPC, Server->Client. Applies JsonData recieved over the wire. </summary>
		/// <param name="msg"> RPC Message info. </param>
		public void SyncJson(RPCMessage msg) {
			string target = msg[0];
			SyncData context = Context(target);
			Client c = msg.sender;
			if (server.isSlave) {

				JsonObject data = Json.TryParse(msg[1]) as JsonObject;
				if (data != null) {
					context.locJson.SetRecursively(data);
				}

			}

		}

		/// <summary> RPC, Server->Client. Clears the given sets of data from the local data set. </summary>
		/// <param name="msg"> RPC Message info. </param>
		public void ClearJson(RPCMessage msg) {
			string target = msg[0];
			SyncData context = Context(target);
			Client c = msg.sender;
			if (c.isSlave) {
				for (int i = 1; i < msg.numArgs; i++) {
					JsonObject obj = context.locJson;
					if (obj.ContainsKey(msg[i])) {
						obj.Remove(msg[i]);
					}
				}

			}
		}

		/// <summary> RPC, Server->Client. Clears all datas from the local data set. </summary>
		/// <param name="msg"> RPC Message Info </param>
		public void ClearAllJson(RPCMessage msg) {
			string target = msg[0];
			SyncData context = Context(target);
			Client c = msg.sender;
			if (c.isSlave) {
				context.locJson.Clear();
			}
		}

	}

	/// <summary> Class for rapidly syncing arbitrary data </summary>
	public class SyncData {
		/// <summary> Local data. Sync to this when accessing data from a client-only context within a script </summary>
		public JsonObject locJson { get; private set; }
		/// <summary> Server data. Sync to this when accessing data from a server-only context within a script </summary>
		public JsonObject srvJson { get; private set; }

		/// <summary> Changes to the server data that need to be reflected on the clients </summary>
		private JsonObject srvDirty;

		/// <summary> Tracks what sets of data clients are subscribed to </summary>
		public ConcurrentDictionary<Client, ConcurrentSet<string>> subscriptions;

		/// <summary> Name of the SyncData context </summary>
		public string name { get; private set; }
		/// <summary> Service attached for data synchronization </summary>
		private SyncService service { get; set; }

		/// <summary> Is the attached server a master/server, or a slave/client? </summary>
		public bool isMaster { get { return service.server.isMaster; } }

		/// <summary> Default set of subscriptions for newly connected clients. </summary>
		public string[] defaultSubs { get; private set; } = { };

		/// <summary> Assigns a set of subscriptions to the defaults for this syncData. 
		/// Should only be called once per instance. Do all such initialization in one place.</summary>
		/// <param name="subs"> Sets to subscribe to by default. </param>
		public void DefaultSubs(params string[] subs) { defaultSubs = subs; }

		public SyncData(SyncService service, string contextName) {
			this.service = service;
			name = contextName;
		}

		public void OnEnable() {
			if (service.server.isMaster) {
				subscriptions = new ConcurrentDictionary<Client, ConcurrentSet<string>>();
				srvJson =  new JsonObject();
				srvDirty = new JsonObject();

			} else {
				locJson = new JsonObject();
			}
		}

		/// <summary> Check this context for any changes, and forward them to any connected/subscribed clients. </summary>
		public void CheckDirty() {
			JsonObject dirtyChanges = new JsonObject();
			dirtyChanges = Interlocked.Exchange(ref srvDirty, dirtyChanges);
			
			if (!dirtyChanges.IsEmpty) {
				foreach (var pair in subscriptions) {
					Client c = pair.Key;
					ConcurrentSet<string> subs = pair.Value;

					JsonObject update = BuildJsonUpdate(c, subs, dirtyChanges);

					if (update.Count > 0) {
						c.Call(service.SyncJson, name, update.ToString());
					}
				}
			}

		}

		/// <summary> Marks data at the given <paramref name="path"/> as dirty, resending updates to any subscribed clients. </summary>
		/// <param name="path"> Path to mark as dirty </param>
		public void SetDirty(string path) {
			if (isMaster) {
				// TBD: Don't use SetData to mark things as dirty- whole idea was to avoid that eventually!
				SetData(path, ReadServer(path));
			}
		}
		/// <summary> Increments the data stored at <paramref name="path"/> by <paramref name="value"/>, considering the value there as a float. </summary>
		/// <param name="path"> path to read/write </param>
		/// <param name="amount"> value to add/subtract from the given path </param>
		public void Increment(string path, float amount) {
			if (isMaster) {
				var val = ReadServer(path).floatVal;
				SetData(path, val + amount);
			}
		}

		public void SetData(JsonObject data) {
			if (isMaster) {
				srvJson = data;
			}
		}

		/// <summary> Sets data at <paramref name="path"/> to be the JSON reflection of <paramref name="value"/> </summary>
		/// <param name="path"> Path in context to store data at </param>
		/// <param name="value"> Object to reflect and store into data </param>
		public void SetData(string path, object value) {
			if (isMaster) {
				// TBD???: Move the main logic of this into an ongoing task, and place information into a queue. ???
				//			-- May be done via refactoring of modules to work via message passing queues
				if (path.Contains('.')) {
					string[] pathSplits = path.Split('.');

					JsonObject srvCursor = srvJson;
					JsonObject drtCursor = srvDirty;

					for (int i = 0; i < pathSplits.Length - 1; i++) {
						string field = pathSplits[i];

						JsonObject srvNext = srvCursor[field] as JsonObject;
						JsonObject drtNext = drtCursor[field] as JsonObject;

						if (srvNext == null) { srvCursor[field] = new JsonObject(); srvNext = srvCursor[field] as JsonObject; }
						if (drtNext == null) { drtCursor[field] = new JsonObject(); drtNext = drtCursor[field] as JsonObject; }

						srvCursor = srvNext;
						drtCursor = drtNext;

					}

					string lastField = pathSplits[pathSplits.Length - 1];
					JsonValue rfl = Json.Reflect(value);
					srvCursor[lastField] = rfl;
					drtCursor[lastField] = rfl;
				} else {
					JsonValue rfl = Json.Reflect(value);
					srvJson[path] = rfl;
					srvDirty[path] = rfl;
				}

			} else {
				throw new Exception("Updates to SyncData can only be done on a Server!");
			}
		}

		/// <summary> Merges <paramref name="changes"/> deltas onto the data at the given <paramref name="path"/>. </summary>
		/// <param name="path"> Path to apply <paramref name="changes"/> to. </param>
		/// <param name="changes"> Changes to apply onto <paramref name="path"/>. </param>
		public void Merge(string path, JsonObject changes) {
			if (service.server.isMaster) {
				if (changes.IsEmpty) { return; }

				Log.Verbose($"Merging SyncData {name} : {path}");

				string[] pathSplits = path.Split('.');

				JsonObject srvCursor = srvJson;
				JsonObject drtCursor = srvDirty;

				for (int i = 0; i < pathSplits.Length - 1; i++) {
					string field = pathSplits[i];

					JsonObject srvNext = srvCursor[field] as JsonObject;
					JsonObject drtNext = drtCursor[field] as JsonObject;

					if (srvNext == null) { srvCursor[field] = new JsonObject(); srvNext = srvCursor[field] as JsonObject; }
					if (drtNext == null) { drtCursor[field] = new JsonObject(); drtNext = drtCursor[field] as JsonObject; }

					srvCursor = srvNext;
					drtCursor = drtNext;

				}

				string lastField = pathSplits[pathSplits.Length - 1];
				JsonObject srvTarget = srvCursor[lastField] as JsonObject ?? new JsonObject();
				JsonObject drtTarget = drtCursor[lastField] as JsonObject ?? new JsonObject();
				foreach (var pair in changes) {
					srvTarget[pair.Key] = pair.Value;
					drtTarget[pair.Key] = pair.Value;
				}
				//JsonValue rfl = Json.Reflect(value);
				srvCursor[lastField] = srvTarget;
				drtCursor[lastField] = drtTarget;

			} else {
				throw new Exception("Updates to SyncData can only be done on a Server!");
			}
		}

		// Todo: Maybe join client/server, since now modules will be separated and 
		// always be individually dedicated to either a client or server, never both at the same time. 

		/// <summary> Reads data from the client cache. </summary>
		/// <param name="path"> Path to read data from </param>
		/// <returns> JsonValue at path within <see cref="locJson"/>, or null if it doesn't exist </returns>
		public JsonValue ReadClient(string path) { return Get(path, locJson); }

		/// <summary> Reads data from the client cache, and interprets it as an object of type <typeparamref name="T"/> </summary>
		/// <typeparam name="T"> Generic Type </typeparam>
		/// <param name="path"> Path to read data from </param>
		/// <param name="defaultValue"> Default value to return if anything fails. </param>
		/// <returns> Data in client cache at <paramref name="path"/>, if it exists, otherwise <paramref name="defaultValue"/> </returns>
		public T PullClient<T>(string path, T defaultValue) {
			var val = ReadClient(path);
			if (val != null) {
				return Json.GetValue<T>(val);
			}
			return defaultValue;
		}


		/// <summary> Reads data from the server cache. </summary>
		/// <param name="path"> Path to read data from </param>
		/// <returns> JsonValue at path within <see cref="srvJson"/>, or null if it doesn't exist </returns>
		public JsonValue ReadServer(string path) { return Get(path, srvJson); }

		/// <summary> Reads data from the server cache, and interprets it as an object of type <typeparamref name="T"/> </summary>
		/// <typeparam name="T"> Generic Type </typeparam>
		/// <param name="path"> Path to read data from </param>
		/// <param name="defaultValue"> Default value to return if anything fails. </param>
		/// <returns> Data in server cache at <paramref name="path"/>, if it exists, otherwise <paramref name="defaultValue"/> </returns>
		public T PullServer<T>(string path, T defaultValue) {
			var val = ReadServer(path);
			if (val != null) {
				return Json.GetValue<T>(val);
			}
			return defaultValue;
		}
		
		/// <summary> Removes a value from the server's data by setting it to null. </summary>
		/// <param name="path"> Path to value to remove. </param>
		public void RemoveServer(string path) { SetData(path, null); }


		/// <summary> Used to read from either the local or server data in ReadServer/ReadClient </summary>
		/// <param name="path"> Path to read </param>
		/// <param name="obj"> Object containing data to read from </param>
		/// <returns> Value at the given path </returns>
		internal static JsonValue Get(string path, JsonObject obj) {
			//if (path == null) { return JsonNull.instance; }

			string[] pathSplits = path.Split('.');
			JsonObject cursor = obj;

			for (int i = 0; i < pathSplits.Length - 1; i++) {
				string field = pathSplits[i];
				JsonObject next = cursor[field] as JsonObject;

				if (next == null) { return JsonNull.instance; }
				cursor = next;
			}

			string lastField = pathSplits[pathSplits.Length - 1];

			return cursor[lastField];
		}

		/// <summary> Called on a client to send an RPC to the server to subscribe them to sets of data. </summary>
		/// <param name="sets"> Names of sets to subscribe to </param>
		public void SubscribeTo(params string[] sets) {
			if (!isMaster) {
				object[] joined = new[] { name }.Concat(sets).ToArray();
				service.server.localClient.Call(service.Subscribe, joined);
			}
		}


		/// <summary> Called on a client to send an RPC to the server to unsubscribe them from sets of data. </summary>
		/// <param name="sets"> Names of sets to unsubscribe from </param>
		public void UnsubscribeTo(params string[] sets) {
			if (!isMaster) {
				object[] joined = new[] { name }.Concat(sets).ToArray();
				service.server.localClient.Call(service.Unsubscribe, joined);
				foreach (string s in sets) {
					if (locJson.ContainsKey(s)) { locJson.Remove(s); }
				}
			}
		}

		/// <summary> Actually subscribes the client to the set of data in this context, and sends a sync update for all sub'd data. </summary>
		/// <param name="c"> Client to subscribe </param>
		/// <param name="sets"> sets of data to subscribe to </param>
		public void DoSubscribe(Client c, params string[] sets) {
			if (!subscriptions.ContainsKey(c)) { subscriptions[c] = new ConcurrentSet<string>(); }

			ConcurrentSet<string> newSets = new ConcurrentSet<string>();
			for (int i = 0; i < sets.Length; i++) {
				string set = sets[i];
				subscriptions[c].Add(set);
				newSets.Add(set);
				// if (isDebug) { Daemon.Print(c.identity + " Subscribed to " + set); }
			}

			JsonObject newStuff = BuildJsonUpdate(c, newSets, srvJson);

			c.Call(service.SyncJson, name, newStuff.ToString());

		}

		/// <summary> Actually unsubscribes client from the set of data in this context. Sends them a message to clean up that data. </summary>
		/// <param name="c"> Client to unsubscribe </param>
		/// <param name="sets"> Sets of data to unsubscribe from. </param>
		public void DoUnsubscribe(Client c, params string[] sets) {

			if (subscriptions.ContainsKey(c)) {
				for (int i = 0; i < sets.Length; i++) {
					string set = sets[i];
					subscriptions[c].Remove(set);
					// Daemon.Log(c.identity + " \\eUnsubbed from " + set, LogLevel.Maximal);
				}
				object[] prams = new[] { name }.Concat(sets).ToArray();
				c.Call(service.ClearJson, prams);
			}
		}

		/// <summary> Actually unsubscribes client from all sets of data in this context. Sends them a message to clean up all data. </summary>
		/// <param name="c"> Client to unsubscribe </param>
		public void DoUnsubscribeAll(Client c) {
			if (subscriptions.ContainsKey(c)) {
				subscriptions[c].Clear();
				c.Call(service.ClearAllJson, name);
			}
		}

		/// <summary> 
		/// Performs any unsubscribing and subscribing that needs to happen to make the client 
		/// subscribed to only the sets in <paramref name="sets"/>, 
		/// each batched into one DoUnsubscribe and DoSubscribe call.
		/// </summary>
		/// <param name="c"> Client to un/subscribe </param>
		/// <param name="sets"> Sets of data the client should be subscribed to. </param>
		public void DoBatchSub(Client c, HashSet<string> sets) {
			if (!subscriptions.ContainsKey(c)) { subscriptions[c] = new ConcurrentSet<string>(); }
			List<string> unSub = new List<string>();
			List<string> toSub = new List<string>();

			var subs = subscriptions[c];
			foreach (var id in subs) {
				if (!sets.Contains(id)) { unSub.Add(id); }
			}
			foreach (var id in sets) {
				if (!subs.Contains(id)) { toSub.Add(id); }
			}

			DoUnsubscribe(c, unSub.ToArray());
			DoSubscribe(c, toSub.ToArray());
		}

		/// <summary> Builds an update message to send to the given client. </summary>
		/// <param name="client"> Client for update to be sent to </param>
		/// <param name="subs"> Sets of data to include in the update </param>
		/// <param name="dirty"> Data that has changed (to build the update against) </param>
		/// <returns> JsonObject containing the relevant data for that client. </returns>
		internal JsonObject BuildJsonUpdate(Client client, ConcurrentSet<string> subs, JsonObject dirty) {

			//StringBuilder str = "";
			//str = str + "Building Update from\n" + dirty.PrettyPrint();

			JsonObject update = new JsonObject(new Dictionary<JsonString, JsonValue>());
			foreach (var sub in subs) {

				// bool applyWhiteList = useWhitelist(client, sub);

				//if (Daemon.logLevel >= LogLevel.Maximal) { str = str + "\n" + sub + "?"; }
				JsonObject set = dirty[sub] as JsonObject;
				if (set != null) {
					if (set["hidden"]) { continue; }
					//if (Daemon.logLevel >= LogLevel.Maximal) { str = str + "Yes! - adding"; }
					// ?????: If there's a concurrency issue here, make it a deepcopy maybe?
					//if (applyWhiteList) {
					//	update[sub] = set.Mask(whitelist);
					//} else {
					//}
					update[sub] = set;

				}
			}
			//Daemon.Log(str, LogLevel.Maximal);
			return update;
		}

	}


}
