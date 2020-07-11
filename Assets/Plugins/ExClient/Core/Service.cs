using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Type used to add custom services to a Server </summary>
	public abstract class Service {

		/// <summary> Debugging flag to allow services to log any method called on them when skipped. </summary>
		public bool LogSkippedMethods = false;

		/// <summary> Owner that this Service belongs to </summary>
		public Server server { get; private set; }

		/// <summary> Is the service enabled (active) ? </summary>
		public bool enabled { get; private set; }
		/// <summary> Is this service running on a master server? </summary>
		public bool isMaster { get { return server.isMaster; } }
		/// <summary> Is this service running on a slave/client server? </summary>
		public bool isSlave { get { return server.isSlave; } }


		/// <summary> Called when the server is started. </summary>
		internal void Started() { OnStart(); }
		/// <summary> Called within AddService() to enable the service </summary>
		internal void Enable() { OnEnable(); enabled = true; }
		/// <summary> Called within RemoveService() to disable the service </summary>
		internal void Disable() { enabled = false; OnDisable(); }
		
		/// <summary> Callback when the server is started. Does not get called if the server is already running. </summary>
		public virtual void OnStart() { }
		/// <summary> Callback when the Service is added to a Servcer </summary>
		public virtual void OnEnable() { }
		/// <summary> Callback when the Service is removed from the server </summary>
		public virtual void OnDisable() { }
		
		/// <summary> Callback every global server tick </summary>
		/// <param name="delta"> Delta between last tick and 'now' </param>
		public virtual void OnTick(float delta) { }

		/// <summary> Callback with a client, called before any <see cref="OnConnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client who has connected. </param>
		public virtual void OnBeganConnected(Client client) { }

		/// <summary> CallCallbacked with a client when that client has connected. </summary>
		/// <param name="client"> Client who has connected. </param>
		public virtual void OnConnected(Client client) { }

		/// <summary> Callback with a client when that client has disconnected. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public virtual void OnDisconnected(Client client) { }
		
		/// <summary> Callback with a client, called after all <see cref="OnDisconnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public virtual void OnFinishedDisconnected(Client client) { }

		/// <summary> Cache of discovered <see cref="OnD"/> Delegates, with their type casted away for easier storage. </summary>
		private Dictionary<Type, Delegate> onTs = new Dictionary<Type, Delegate>();

		/// <summary> Delegate type to ease Delegates into a cache </summary>
		// <typeparam name="T"> Generic type of parameter </typeparam>
		/// <param name="val"> Delegate Parameter </param>
		//private delegate void OnD<T>(T val);
		private delegate void OnD<T>(T val);

		/// <summary> Dynamically call a method based off a type value.  </summary>
		/// <typeparam name="T"> Generic type of parameter </typeparam>
		/// <param name="val"> Parameter to find method for (Eg, On(Int32 val) or OnInt32(Int32 val). </param>
		public void DoOn<T>(T val) {
			Type type = val.GetType();
			if (!onTs.ContainsKey(type)) { RegisterOnType(val); }
			if (onTs[type] != null) {
				try {
					// @Speed: Object[] creation for dynamic invocation.... may be slow if overused?
					// onTs[type].Method.Invoke(this, new object[] { val });
					// @Ick: Next line.
					((OnD<T>) onTs[type])(val);

				} catch (Exception e) {
					Log.Error($"{nameof(Service)}: Error during dynamic On<{type}>(): ", e);
				}
			} else {

				if (LogSkippedMethods) {
					Log.Info($"{nameof(Service)}: Skipping On<{type}> in {GetType()}");

				}
			}
			
		}
		
		/// <summary> Register method for type of T if it exists, otherwise register null </summary>
		/// <typeparam name="T"> Generic type  </typeparam>
		/// <param name="val"> Parameter for type consistency </param>
		private void RegisterOnType<T>(T val) {
			Type valType = val.GetType();
			Type[] typeParams = new Type[] { valType };
			onTs[valType] = null;
			List<Type> chain = TypeChain(GetType());
			Type actionType = typeof(OnD<T>);

			foreach (var type in chain) {
				try {
					MethodInfo mi1 = type.GetMethod("On", typeParams);
					if (mi1 != null) {
						onTs[valType] = mi1.CreateDelegate(actionType, this);

						return;
					}
					MethodInfo mi2 = type.GetMethod("On"+type.ShortName(), typeParams);
					if (mi2 != null) {
						onTs[valType] = mi2.CreateDelegate(actionType, this);
						return;
					}

				} catch (Exception e) { 
					Log.Error($"{nameof(Service)} Failed to register dynamic On<{valType}>() with {type}: ", e); 
				}
			}
		}

		/// <summary> Get a list of all types that T inherits from </summary>
		/// <param name="t"> Type to iterate </param>
		/// <returns> List of all types, in order, that T inherits from (including T itself) </returns>
		private static List<Type> TypeChain(Type t) {
			List<Type> types = new List<Type>();
			types.Add(t);

			Type temp = t;
			while (temp.BaseType != null) {
				temp = temp.BaseType;
				types.Add(temp);
			}

			return types;
		}

		/// <summary> Adds service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to add. </typeparam>
		/// <returns> Service that was added. </returns>
		/// <exception cref="Exception"> if any service with conflicting type or name exists. </exception>
		public T AddService<T>() where T : Service { return server.AddService<T>(); }

		/// <summary> Removes service of type <typeparamref name="T"/>. </summary>
		/// <typeparam name="T"> Type of service to remove </typeparam>
		/// <returns> True if removed, false otherwise. </returns>
		public bool RemoveService<T>() where T : Service { return server.RemoveService<T>(); }

		/// <summary> Gets a service with type <typeparamref name="T"/> </summary>
		/// <typeparam name="T"> Type of service to get </typeparam>
		/// <returns> Service of type <typeparamref name="T"/> if present, otherwise null. </returns>
		public T GetService<T>() where T : Service { return server.GetService<T>(); }
	}	

	/// <summary> Holds a way to access instance members of a type without needing to explicitly create instances. </summary>
	/// <typeparam name="T">Generic type </typeparam>
	/// <remarks> This is simply a convinience class to access instance OnMessage callbacks. </remarks>
	public sealed class Members<T> {
		/// <summary> Generic instance. Do not expect this instance to be valid to operate on. </summary>
		public static readonly T i = Activator.CreateInstance<T>();
	}

	/// <summary> Service template class. Intended for copy/pasting to create a new service. </summary>
	public class ServiceTemplate : Service {
		/// <summary> Callback when the Service is added to a Servcer </summary>
		public override void OnEnable() { }
		/// <summary> Callback when the Service is removed from the server </summary>
		public override void OnDisable() { }

		/// <summary> Callback every global server tick </summary>
		/// <param name="delta"> Delta between last tick and 'now' </param>
		public override void OnTick(float delta) { }

		/// <summary> Callback with a client, called before any <see cref="OnConnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client who has connected. </param>
		public override void OnBeganConnected(Client client) { }

		/// <summary> CallCallbacked with a client when that client has connected. </summary>
		/// <param name="client"> Client who has connected. </param>
		public override void OnConnected(Client client) { }

		/// <summary> Callback with a client when that client has disconnected. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public override void OnDisconnected(Client client) { }

		/// <summary> Callback with a client, called after all <see cref="OnDisconnected(Client)"/> calls have finished. </summary>
		/// <param name="client"> Client that has disconnected. </param>
		public override void OnFinishedDisconnected(Client client) { }
	}
}
