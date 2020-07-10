using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ex.Utils;

namespace Ex {

	/// <summary> Delegate type for encryption/decryption. </summary>
	/// <param name="source"> Bytes to be encrypted/decrypted </param>
	/// <returns> Encrypted/decrypted version of source </returns>
	public delegate byte[] Crypt(byte[] source);

	public static class ThreadUtil {
		/// <summary> Pause for the given <paramref name="ms"/> or yield. </summary>
		/// <param name="ms"> Ms to pause for, or if &lt;= 0, yield.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Hold(int ms) { 
			if (ms <= 0) { Thread.Yield(); } 
			else { Thread.Sleep(ms); } 
		}
		/// <summary> Kill a thread if and only if it is not the active thread. </summary>
		/// <param name="t"> Thread to try to kill </param>
		/// <returns> Returns true if an abort was attempted, false if it was the current thread or null. </returns>
		public static bool TerminateIfNotActive(this Thread t) {
			if (t != null && t != Thread.CurrentThread) {
				try { t.Abort(); } catch { }
				return true;
			}
			return false;
		}
		/// <summary> Try to abort all passed threads. </summary>
		/// <param name="threads"> Threads to terminate </param>
		public static void TerminateAll(params Thread[] threads) {
			foreach (var thread in threads) { TerminateIfNotActive(thread); }
			if (threads.Contains(Thread.CurrentThread)) { Thread.CurrentThread.Abort(); }
		}
	}

	/// <summary> Primary class for ExServer </summary>
	public class Server {
		
		/// <summary> Null instance static constructor for servers. Used when stuff is running on a 'local' client. </summary>
		public static Server NullInstance { get { return new Server(-1); } }
		/// <summary> Helper function to create a thread with priority </summary>
		/// <param name="start"> Insertion point </param>
		/// <param name="priority"> Priority of thread </param>
		/// <returns> Created and started thread. </returns>
		private static Thread StartThread(ThreadStart start, ThreadPriority priority = ThreadPriority.Normal) {
			Thread t = new Thread(start);
			t.Name = start.GetMethodInfo().Name + " Thread";
			t.Priority = priority;
			t.Start();

			return t;
		}

		/// <summary> Update thread. </summary>
		private Thread globalUpdateThread;
		/// <summary> Thread for listening for new connections. </summary>
		private Thread listenThread;
		/// <summary> Thread for sending information. </summary>
		private Thread mainSendThread;
		/// <summary> Thread for recieving information. </summary>
		private Thread mainRecrThread;

		/// <summary> Collection of services keyed by type. </summary>
		public ConcurrentDictionary<Type, Service> services { get; private set; }
		/// <summary> Collection of services keyed by name.  </summary>
		public ConcurrentDictionary<string, Service> servicesByName { get; private set; }
		/// <summary> Connections, keyed by client ID </summary>
		public ConcurrentDictionary<Guid, Client> connections { get; private set; }
		/// <summary> Cache of message delegates </summary>
		public Dictionary<string, RPCMessage.Handler> rpcCache { get; private set; }

		/// <summary> Queue used to hold all clients for checking for sending data </summary>
		private ConcurrentQueue<Client> sendCheckQueue;
		/// <summary> Queue used to hold all clients for checking for recieving data </summary>
		private ConcurrentQueue<Client> recrCheckQueue;

		/// <summary> Incoming messages. </summary>
		private ConcurrentQueue<RPCMessage> incoming;
		/// <summary> Actions to do later </summary>
		private ConcurrentQueue<Action> doLater = new ConcurrentQueue<Action>();
		
		/// <summary> Command runner </summary>
		// public Cmdr commander { get; private set; }

		/// <summary> Server port. Negative means 'slave server' or client-sided. </summary>
		public int port { get; private set; }
		/// <summary> Update ticks per second </summary>
		public float tick { get; private set; }
		/// <summary> Milliseconds per tick </summary>
		public float tickRate { get; private set; }

		/// <summary> Slave server. Port is negative. </summary>
		public bool isSlave { get { return port < 0; } }
		/// <summary> Master server. Port is non-negative. </summary>
		public bool isMaster { get { return port >= 0; } }

		/// <summary> Is the server currently running? </summary>
		public bool Running { get; private set; }
		/// <summary> Is the server attempting to stop running? </summary>
		public bool Stopping { get; private set; }

		/// <summary> TCP listener </summary>
		private TcpListener listener;

		/// <summary> UTC Timestamp of last tick </summary>
		private DateTime lastTick;

		/// <summary> Hidden reference to local client for slave server </summary>
		private Client _localClient;
		/// <summary> Returns local client object, if this is a slave server. </summary>
		public Client localClient { get { return isSlave ? _localClient : null; } }
		
		/// <summary> Creates a Server with the given port and tickrate. </summary>
		public Server(int port = 32055, float tick = 50) {
			if (port == ushort.MaxValue-2 || (port >= 0 && port < 1024)) {
				throw new ArgumentException($"Cannot use a port of {port}. Please avoid system ports or ports with neighbors that are in use, or near the max port number.");
			}
			sendCheckQueue = new ConcurrentQueue<Client>();
			recrCheckQueue = new ConcurrentQueue<Client>();
			incoming = new ConcurrentQueue<RPCMessage>();

			servicesByName = new ConcurrentDictionary<string, Service>();
			services = new ConcurrentDictionary<Type, Service>();
			connections = new ConcurrentDictionary<Guid, Client>();

			rpcCache = new Dictionary<string, RPCMessage.Handler>();
			//commander = new Cmdr();
			this.port = port;

			this.tick = tick;
			tickRate = 1000.0f / tick;
			
			Stopping = false;
			Running = false;

			AddService<CoreService>();
			lastTick = DateTime.UtcNow;
		}

		public void Start() {
			
			Running = true;
			if (isMaster) {
				listenThread = StartThread(Listen);
			}

			globalUpdateThread = StartThread(GlobalUpdate);
			mainSendThread = StartThread(SendLoop);
			mainRecrThread = StartThread(RecrLoop);
		}

		public void Stop() {
			if (!Running || Stopping) { return; }
			/// Set flags and push to RAM.
			Running = false;
			Stopping = true;
			Thread.MemoryBarrier();
			
			/// Wait for threads to finish their work
			listener?.Stop();
			globalUpdateThread?.Join();
			listenThread?.Join();
			mainSendThread?.Join();
			mainRecrThread?.Join();

			/// Stop all services 
			List<Type> servicesToStop = new List<Type>();
			foreach (var pair in services) { servicesToStop.Add(pair.Key); }
			for (int i = servicesToStop.Count-1; i >= 0; i--) { RemoveService(servicesToStop[i]); }

			/// Clean up on slave: just tell the server you closed.
			if (isSlave) {
				localClient.Call(Members<CoreService>.i.Closed);
				SendData(localClient);
				Close(localClient);
			} else {

				/// @Todo: Cleanup master by sending packet to all clients: server is closed
				List<Client> toClose = connections.Values.ToList();
				foreach (var client in toClose) {
					Close(client);
				}

			}
			
			Stopping = false;
			Thread.MemoryBarrier();

		}

		/// <summary> Gets the client that has the given guid </summary>
		/// <param name="id"> ID of client to get </param>
		/// <returns> Client for ID, if present, or null if none exists. </returns>
		public Client GetClient(Guid id) { return connections.ContainsKey(id) ? connections[id] : null; }

		/// <summary> Hooks up details of client so server can handle communication.
		/// Exposed to allow slave clients to explicitly connect to their server. </summary>
		/// <param name="client"> Slave client to connect. </param>
		public void OnConnected(Client client) {
			if (isMaster) {
				connections[client.id] = client;
			} else {
				_localClient = client;
			}
			
			foreach (var service in services.Values) {
				try {
					service.OnBeganConnected(client);
				} catch (Exception e) { Log.Error($"Error in OnBeganConnected for {service.GetType()}", e); }
			}

			foreach (var service in services.Values) {
				try {
					service.OnConnected(client);
				} catch (Exception e) { Log.Error($"Error in OnConnected for {service.GetType()}", e); }
			}

			sendCheckQueue.Enqueue(client);
			recrCheckQueue.Enqueue(client);
		}

		/// <summary>  Globally calls an <see cref="RPCMessage"/>, as if the given client had sent it. </summary>
		/// <param name="client"> Client to simulate call of method for </param>
		/// <param name="callback"> Callback to call </param>
		/// <param name="stuff"> Parameters for call </param>
		public void Call(Client client, RPCMessage.Handler callback, params System.Object[] stuff) {
			string str = Client.FormatCall(callback, stuff);
			RPCMessage msg = RPCMessage.TCP(client, str);
			incoming.Enqueue(msg);
		}

		/// <summary> Closes client from being tracked by the server. 
		/// Exposed to allow slave clients to explicitly disconnect from their server. </summary>
		/// <param name="client"> Slave client to connect. </param>
		public void Close(Client client) {
			if (!client.closed) {
				
				foreach (var service in services.Values) {
					try {
						service.OnDisconnected(client);
					} catch (Exception e) { Log.Error($"Error in OnDisconnected for {service.GetType()}", e); }
				}
				foreach (var service in services.Values) {
					try {
						service.OnFinishedDisconnected(client);
					} catch (Exception e) { Log.Error($"Error in OnFinishedDisconnected for {service.GetType()}", e); }
				}

				client.Close();
			}
		}

		/// <summary> Called to send an internal event message for any service to act on </summary>
		/// <typeparam name="T"> Generic type of message to send </typeparam>
		/// <param name="val"> Value to send as message </param>
		public void On<T>(T val) {
			doLater.Enqueue(() => DoLater(val));
		}

		/// <summary> Passes a message to all services </summary>
		/// <typeparam name="T"> Generic type of event message </typeparam>
		/// <param name="val"> Event message </param>
		private void DoLater<T>(T val) {
			foreach (var service in services.Values) {
				service.DoOn(val);
			}
		}

		/// <summary> Handles internal events, called every global tick after handling RPC messages. </summary>
		private void HandleInternalEvents() {
			Action action; 
			while (doLater.TryDequeue(out action)) {
				try {
					action();
				} catch (Exception e) {
					Log.Error("Error during event", e);
				}
			}
		}

		private void Listen() {
			while (Running) {
				try {
					listener = new TcpListener(IPAddress.Any, port);
					listener.Start();
					Log.Info("\\eListening for clients...");
					
					while (true) {
						TcpClient tcpClient = listener.AcceptTcpClient();
						Client client = new Client(tcpClient, this);
						OnConnected(client);
					}
					
				}
				catch (SocketException se) {
					if (se.ErrorCode == unchecked((int)0x80004005)) {
						// Blocking operation was explictly stopped via cancel.
						Log.Info("\\eDetected WSACancelBlockingCall");
						break;
					}
				}
				catch (Exception e) {
					Log.Error(e, "Socket Listener had internal failure. Retrying.");
				}
			}
		}

		
		private void GlobalUpdate() {
			while (Running) {
				try {

					RPCMessage msg;
					while (incoming.TryDequeue(out msg)) {
						if (msg.sender.closed) {
							// early break???
							// maybe we still want to handle messages when closed 
							//		eg, ServerShuttingDown
						}
						HandleMessage(msg);
					}

					HandleInternalEvents();

				} catch (Exception e) {
					Log.Error("Failure in Server.GlobalUpdate during Handlers: ", e);
				}

				DateTime now = DateTime.UtcNow;
				TimeSpan diff = now - lastTick;
				try {
					if (diff.TotalMilliseconds > tickRate) {
						lastTick = now;
						float d = (float) diff.TotalSeconds;
						foreach (var pair in services) {
							pair.Value.OnTick(d);
						}
					}
				} catch (Exception e) {
					Log.Error("Failure in Server.GlobalUpdate during Ticks: ", e);
				}

				
				ThreadUtil.Hold(1);
			}
			string id = isSlave ? "Slave" : "Master";
			Log.Info($"Updates stopping for {id} and cleaning up.");
			//Todo: Cleanup work
		}

		/// <summary> Creates an Update thread bound to the lifetime of the server, or until the inner code wants to stop.  </summary>
		/// <param name="body"> Body of code to execute, expected to return true until it wants to stop </param>
		/// <param name="rate"> Milliseconds to wait between loops </param>
		/// <param name="priority"> Priority to give to the created thread </param>
		/// /// <param name="stopOnError"> If an error occurs internal to the function, should it terminate the thread? </param>
		/// <returns> Thread object looping the given code body. </returns>
		public Thread CreateUpdateThread(Func<bool> body, int? rate = null, bool stopOnError = false, ThreadPriority priority = ThreadPriority.Normal) {
			return StartThread(Loop(body, rate, stopOnError), priority);
		}

		/// <summary> Creates a thread that loops a specific body, at a specific rate.</summary>
		/// <param name="body"> code to execute, returns true each cycle to continue, and false if it wants to stop itself. </param>
		/// <param name="rate"> Milliseconds to wait between loops </param>
		/// <param name="stopOnError"> If an error occurs internal to the function, should it terminate the thread? </param>
		/// <returns> ThreadStart delegate wrapping body/rate parameters. </returns>
		public ThreadStart Loop(Func<bool> body, int? rate = null, bool stopOnError = false) {
			// We are implicitly capturing function params, but,
			// since this should not be used very often, should not cause memory leak. 
			return () => {
				DateTime last = DateTime.UtcNow;
				while (Running) {
					DateTime now = DateTime.UtcNow;
					try {
						bool keepGoing = body();
						if (!keepGoing) { break; }
					} catch (Exception e) {
						Log.Error("Failure in Server.Loop", e);
						if (stopOnError) { break; }

					}
					ThreadUtil.Hold(rate ?? 1);
				}

			};

		}


		/// <summary> Loop for sending data to connected clients. @Todo: Pool this. </summary>
		private void SendLoop() {
			while (Running) {
				try {
					Client c;
					if (sendCheckQueue.TryDequeue(out c)) {
						SendData(c);
						if (!c.closed) { sendCheckQueue.Enqueue(c); }
					}

				} catch (Exception e) {
					Log.Error("Failure in Server.SendLoop", e);
				}
				ThreadUtil.Hold(1);
			}
			string id = isSlave ? "Slave" : "Master";
			Log.Info($"SendLoop Ending for {id}");
		}

		/// <summary> Loop for recieving data from connected clients. @Todo: Pool this. </summary>
		private void RecrLoop() {
			while (Running) {
				try {
					Client c;
					if (recrCheckQueue.TryDequeue(out c)) {
						RecieveData(c);
						if (!c.closed) { recrCheckQueue.Enqueue(c); } 
					}
					
				} catch (Exception e) {
					Log.Error("Failure in Server.RecrLoop", e);
				}
				ThreadUtil.Hold(1);
			}
			string id = isSlave ? "Slave" : "Master";
			Log.Info($"RecrLoop Ending for {id}");
		}

		/// <summary> Sends all pending messages to a client. </summary>
		/// <param name="client"> Client to send data for </param>
		public void SendData(Client client) {
			// Todo: Enable Pokeing client with 0 bytes occasionally.
			// DateTime now = DateTime.UtcNow;
			string msg = null;

			if (client.udp != null) {
				try {
					while (!client.closed && client.udpOutgoing.TryDequeue(out msg)) {

						msg += RPCMessage.EOT;
						byte[] message = msg.ToBytesUTF8();
						message = client.enc(message);
						int ret = client.udp.SendTo(message, client.remoteUdpHost);
						Log.Verbose($"Client {client.identity} yeeted message {msg} : {ret}");

					}
					if (!client.closed) {
					
					}
				} catch (Exception e) {
					Log.Warning($"Server.SendData(Client): Error during UDP send: {e.GetType()}. Will defer to TCP closure to disconnect.", e);
				}
			}

			try {
				while (!client.closed && client.tcpOutgoing.TryDequeue(out msg)) {
					Log.Verbose($"Client {client.identity} sending message {msg}");

					msg += RPCMessage.EOT;
					byte[] message = msg.ToBytesUTF8();
					message = client.enc(message);
					
					client.tcpStream.Write(message, 0, message.Length);
				}
			} catch (ObjectDisposedException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (SocketException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (IOException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (InvalidOperationException e) {
				Log.Verbose($"Server.SendData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (Exception e) {
				Log.Warning("Server.SendData(Client): ", e);
			}


		}

		const int UDP = 1;
		const int TCP = 0;
		/// <summary> Attempts to read data from a client once </summary>
		/// <param name="client"> Client information to read data for </param>
		public void RecieveData(Client client) {
			// Helper method to handle reading 
			Client.ReadState read(Client.ReadState state, int kind) {
				if (state.bytesRead > 0) {
					state.message = state.buffer.Chop(state.bytesRead);
					state.message = client.dec(state.message);
					string str = state.message.GetStringUTF8();

					state.held += str;
					int index = state.held.IndexOf(RPCMessage.EOT);
					while (index >= 0) {
						string pulled = state.held.Substring(0, index);
						state.held = state.held.Remove(0, index + 1);
						index = state.held.IndexOf(RPCMessage.EOT);

						if (pulled.Length > 0) {
							RPCMessage msg = kind == UDP ? RPCMessage.UDP(client, pulled) : RPCMessage.TCP(client, pulled);
							incoming.Enqueue(msg);
						}
					}
				}
				return state;
			}

			if (client.udp != null) {
				try {

					bool canReadUDP = !client.closed && client.udp.Available > 0;
					EndPoint ep = client.remoteUdpHost;
					client.udpReadState.bytesRead = canReadUDP
						? client.udp.ReceiveFrom(client.udpReadState.buffer, ref ep)
						: -1;
					client.udpReadState = read(client.udpReadState, UDP);
					if (canReadUDP && client.udpReadState.bytesRead > 0 && ep is IPEndPoint) {
						client.remoteUdpHost = (IPEndPoint)ep;
						Log.Info($"{client.identity} recieved from {client.remoteUdpHost}");
					}
				
				} catch (Exception e) {
					Log.Warning($"Server.RecieveData(Client): {client.identity} Error during UDP read. {e.GetType()}. Will defer to TCP closure to disconnect.", e);
				}

			}
					

			try {
				
				client.tcpReadState.bytesRead = !client.closed && client.tcpStream.CanRead && client.tcpStream.DataAvailable
					? client.tcpStream.Read(client.tcpReadState.buffer, 0, client.tcpReadState.buffer.Length)
					: -1;
				client.tcpReadState = read(client.tcpReadState, TCP);

			} catch (ObjectDisposedException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (SocketException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably Disconnected. {e.GetType()}", e);
				Close(client);
			} catch (IOException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (InvalidOperationException e) {
				Log.Verbose($"Server.RecieveData(Client): {client.identity} Probably timed out. {e.GetType()}", e);
				Close(client);
			} catch (Exception e) {
				Log.Warning($"Server.RecieveData(Client): ", e);
			}
		}

		private void HandleMessage(RPCMessage msg) {
			RPCMessage.Handler handler = GetHandler(msg);
			
			try {
				handler?.Invoke(msg);
			} catch (Exception e) {
				Log.Warning($"Error occurred during {msg.rpcName}: ", e);
			}
			
		}

		/// <summary> Parameter Signature of a <see cref="RPCMessage"/> handler method. </summary>
		private static Type[] SIGNATURE_OF_MESSAGEHANDLER = new Type[] { typeof(RPCMessage) };

		/// <summary> Cache all of the <see cref="RPCMessage"/> handlers in the given <see cref="Service"/> </summary>
		/// <param name="service"> Service to cache </param>
		private void LoadCache(Service service) {
			Type t = service.GetType();
			var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

			foreach (var method in methods) {
				if (MatchesSig(method, SIGNATURE_OF_MESSAGEHANDLER) && method.ReturnType.Equals(typeof(void))) {
					var handler = (RPCMessage.Handler)method.CreateDelegate(typeof(RPCMessage.Handler), service);
					var rpcName = t.ShortName() + "." + method.Name;
					rpcCache[rpcName] = handler;
					// Log.Verbose($"Loaded RPC {rpcName}");
				}
			}
		}
		/// <summary> Check if the given <see cref="MethodInfo"/> has the given parameter <paramref name="signature"/> </summary>
		/// <param name="method"> Method to check </param>
		/// <param name="signature"> Signature to check for </param>
		/// <returns> true if the <paramref name="method"/>'s parameter's match <paramref name="signature"/></returns>
		private static bool MatchesSig(MethodInfo method, Type[] signature) {
			var param = method.GetParameters();
			if (param.Length != signature.Length) { return false; }
			for (int i = 0; i < signature.Length; i++) {
				if (signature[i] != param[i].ParameterType) { return false; }
			}
			return true;
		}

		/// <summary> Gets the <see cref="RPCMessage.Handler"/> from the cache for the given <see cref="RPCMessage"/>.
		/// If it does not yet exist, creates a handler for that method. </summary>
		/// <param name="msg"> Message to get the handler for </param>
		/// <returns> <see cref="RPCMessage.Handler"/> that should be used to handle the given <paramref name="msg"/>. </returns>
		private RPCMessage.Handler GetHandler(RPCMessage msg) {
			string rpcName = msg.rpcName;
			if (rpcCache.ContainsKey(rpcName)) {
				return rpcCache[rpcName];
			}

			if (!servicesByName.ContainsKey(msg.serviceName)) {
				Log.Warning($"No service for name [{msg.serviceName}] is registered.");
				return null;
			}

			Service service = servicesByName[msg.serviceName];
			Type type = service.GetType();
			MethodInfo method = type.GetMethod(msg.methodName);

			if (method != null) {
				var handler = (RPCMessage.Handler)method.CreateDelegate(typeof(RPCMessage.Handler), service);
				rpcCache[rpcName] = handler;

			}
			
			Log.Warning($"No method [{msg.rpcName}] found.");
			return null;
		}

		#region SERVICES

		/// <summary> MethodInfo pointing to setter on the <see cref="Service.server"/> property </summary>
		private static MethodInfo SET_OWNER_METHODINFO = typeof(Service).GetProperty("server", typeof(Server)).GetSetMethod(true);
		/// <summary> Cached object array for MethodInfo invocation without extra garbage collection. </summary>
		private object[] SET_OWNER_ARGS;
		/// <summary> Adds service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to add. </typeparam>
		/// <returns> Service that was added. </returns>
		/// <exception cref="Exception"> if any service with conflicting type or name exists. </exception>
		public T AddService<T>() where T : Service {
			if (SET_OWNER_ARGS == null) { SET_OWNER_ARGS = new object[] { this }; }
			Type type = typeof(T);
			string typeName = type.ShortName();
			if (services.ContainsKey(type)) {
				throw new Exception($"ExServer: Attempt made to add duplicate service {type}.");
			}
			if (servicesByName.ContainsKey(typeName)) {
				throw new Exception($"ExServer: Attempt made to add a service with a duplicate name [{typeName}] by {type}.");
			}

			T service = Activator.CreateInstance<T>();
			SET_OWNER_METHODINFO.Invoke(service, SET_OWNER_ARGS);
			services[type] = service;
			servicesByName[typeName] = service;
			service.Enable();
			Log.Verbose($"Enabled Service with type=[{type}] name=[{typeName}]");
			LoadCache(service);

			return service;
		}

		/// <summary> Removes service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to remove </typeparam>
		/// <returns> True if removed, false otherwise. </returns>
		public bool RemoveService<T>() where T : Service {
			if (services.ContainsKey(typeof(T))) {
				Service removed = services[typeof(T)];
				removed.Disable();

				Type type = typeof(T);
				servicesByName.Remove(type.ShortName());
				services.Remove(type);

				return true;
			}
			return false;
		}

		/// <summary> Removes service of the given type. </summary>
		/// <param name="t"> Type of service to remove </param>
		/// <returns> True if removed, false otherwise. </returns>
		public bool RemoveService(Type t) {
			if (t != null 
				&& typeof(Service).IsAssignableFrom(t)
				&& services.ContainsKey(t)) {
				
				Service removed = services[t];
				removed.Disable();
				
				servicesByName.Remove(t.ShortName());
				services.Remove(t);

				return true;
			}
			return false;
		}

		/// <summary> Gets a service with type <typeparamref name="T"/> </summary>
		/// <typeparam name="T"> Type of service to get </typeparam>
		/// <returns> Service of type <typeparamref name="T"/> if present, otherwise null. </returns>
		public T GetService<T>() where T : Service {
			if (services.ContainsKey(typeof(T))) { return (T)services[typeof(T)]; }
			return null;
		}
		#endregion


	}
	/// <summary> Helpers for the server class. </summary>
	public static class ServerUtils {

		/// <summary> Gets the short name of a given type. </summary>
		/// <param name="t"> Type to get the name </param>
		/// <returns> Final, short name of the given type. </returns>
		public static string ShortName(this Type t) {
			string name = t.Name;
			if (name.Contains('.')) {
				return name.Substring(name.LastIndexOf('.'));
			}
			return name;
		}

		/// <summary> Take a copy of a sub-region of a byte[] </summary>
		/// <param name="array"> byte[] to chop </param>
		/// <param name="size"> maximum size of resulting sub-array </param>
		/// <param name="start"> start index </param>
		/// <returns> sub-region from given byte[], of max length <paramref name="size"/> starting from index <paramref name="start"/> in the original <paramref name="array"/>. </returns>
		public static byte[] Chop(this byte[] array, int size, int start = 0) {
			if (start >= array.Length) { return null; }
			if (size + start > array.Length) {
				size = array.Length - start;
			}
			byte[] chopped = new byte[size];
			for (int i = 0; i < size; i++) {
				chopped[i] = array[i + start];
			}
			return chopped;
		}
		static Encoding utf8 = Encoding.UTF8;
		static Encoding ascii = Encoding.ASCII;
		/// <summary> Turns a string into a byte[] using ASCII </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by ASCII encoding </returns>
		public static byte[] ToBytes(this string s) { return ascii.GetBytes(s); }
		/// <summary> Turns a string into a byte[] using UTF8 </summary>
		/// <param name="s"> String to convert </param>
		/// <returns> Internal byte[] by UTF8 encoding </returns>
		public static byte[] ToBytesUTF8(this string s) { return utf8.GetBytes(s); }

		/// <summary> Reads a byte[] as if it is an ASCII encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> ASCII string created from the byte[] </returns>
		public static string GetString(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return ascii.GetString(b, 0, length);
		}

		/// <summary> Reads a byte[] as if it is a UTF8 encoded string </summary>
		/// <param name="b"> byte[] to read </param>
		/// <param name="length"> length to read </param>
		/// <returns> UTF8 string created from the byte[] </returns>
		public static string GetStringUTF8(this byte[] b, int length = -1) {
			if (length == -1) { length = b.Length; }
			return utf8.GetString(b, 0, length);
		}
	}
}
