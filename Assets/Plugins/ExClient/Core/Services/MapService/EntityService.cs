#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#else
using UnityEngine;
#endif
using Ex.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Ex {

	/// <summary> Attribute to be applied to <see cref="Comp"/> classes that are server-only </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ServerOnlyComponentAttribute : Attribute { }
	/// <summary> Attribute to be applied to <see cref="Comp"/> classes that are hidden from clients that do not 'own' the component. </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class OwnerOnlySyncAttribute : Attribute { }

	/// <summary> Service which manages the creation and tracking of entities. 
	/// Entities are automatically created for connecting clients and removed for disconnecting ones.  </summary>
	public class EntityService : Service {
		#region MESSAGE_STRUCTS
		/// <summary> Message sent when an entity was created. </summary>
		public struct EntitySpawned { public Guid id; }
		/// <summary> Message sent when an entity was removed. </summary>
		public struct EntityDespawned { public Guid id; }
		/// <summary> Message sent when an entity has a new component. </summary>
		public struct ComponentAdded { public Guid id; public Type componentType; }
		/// <summary> Message sent when an entity has new data. </summary>
		public struct ComponentChanged { public Guid id; public Type componentType; }
		/// <summary> Message sent when an entity no longer has a component. </summary>
		public struct ComponentRemoved { public Guid id; public Type componentType; }

		/// <summary> Message sent when an entity is marked as the local entity. Sent once when the entity is first sync'd to its own client. </summary>
		public struct SetLocalEntity { public Guid id; }
		#endregion
		

#if !UNITY
		[BsonIgnoreExtraElements]
		public class UserEntityInfo : DBEntry {

			public string map { get; set; }
			public Vector3 position { get; set; }
			public Vector3 rotation { get; set; }

		}
		
		public void InitializeEntityInfo(Guid userID) {
			
			Log.Info($"Initializing EntityInfo for {userID}");
			UserEntityInfo info = new UserEntityInfo();
			info.position = Vector3.zero;
			info.rotation = Vector3.zero;
			info.map = "Limbo";
			info.guid = userID;
			
			GetService<DBService>().Save(info);
			Log.Info($"Saved EntityInfo for {userID}");

			var check = GetService<DBService>().Get<UserEntityInfo>(userID);
			Log.Info($"Retrieved saved info? {check}");

			
		}
		
		public const bool DEBUG_TYPES = true;

		/// <summary> EntityInfo (spawn source) data cached from database by type </summary>
		public ConcurrentDictionary<string, EntityInfo> entityInfos;
		public EntityInfo GetEntityInfo(string typeName) {
			if (entityInfos.ContainsKey(typeName)) { return entityInfos[typeName]; }
			var info = (entityInfos[typeName] = GetService<DBService>().Get<EntityInfo>("Content", "type", typeName));
			
			if (DEBUG_TYPES) {
				Log.Debug($"Baking type info into {typeof(Ex.Typed).FullName} for {typeName}");
				var comps = new ComponentInfo[info.components.Length + 1];
				for (int i = 0; i < info.components.Length; i++) {
					comps[i] = info.components[i];
				}
				// Bake some type information for seeing what type was loaded to form an entity.
				// This should be the only extra information after what is already in the entity database.
				var typed = new ComponentInfo();
				typed.type = typeof(Ex.Typed).FullName;
				typed.data = new MongoDB.Bson.BsonDocument();
				typed.data["type"] = typeName;
				comps[comps.Length-1] = typed;
				info.components = comps;
			}

			return info;
		}
#endif

		/// <summary> Current set of entities. </summary>
		private ConcurrentDictionary<Guid, Entity> entities;
		/// <summary> Components for entities </summary>
		public ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>> componentTables;
		/// <summary> Subscriptions. Entity IDs a client is subscribed to. server only. </summary>
		public ConcurrentDictionary<Client, ConcurrentSet<Guid>> subscriptions;
		/// <summary> Subscribers. Clients that are subscribed to an Entity ID. server only. </summary>
		public ConcurrentDictionary<Guid, ConcurrentSet<Client>> subscribers;

		/// <summary> Holds pre-processed type information for a given component type. </summary>
		private class TypeInfo {
			/// <summary> Type of cached information </summary>
			public Type type;
			/// <summary> Names of fields that are sync'd </summary>
			public FieldInfo[] syncedFields;
			/// <summary> Cached setter functions </summary>
			public Delegate[] syncedFieldSetters;
			/// <summary> Cached getter functions </summary>
			public Delegate[] syncedFieldGetters;
		}
		/// <summary> Types of components </summary>
		private ConcurrentDictionary<string, TypeInfo> componentTypes;


		/// <summary> Gets an entity by ID, or null if none exist. </summary>
		/// <param name="id"> ID of entity to try and get </param>
		/// <returns> Entity for ID, or null if no entity exists for that ID </returns>
		public Entity this[Guid id] {
			get { 
				Entity e;
				if (entities.TryGetValue(id, out e)) { return e; }
				return null;
			}
		}
		
		/// <summary> Creates a new entity, and returns the reference to it. </summary>
		/// <returns> Reference to the newly created entity </returns>
		public Entity CreateEntity(Guid? id = null) {
			Entity entity = (id == null) ? new Entity(this) : new Entity(this, id.Value);
			entities[entity.guid] = entity;

			if (isMaster) {
				subscribers[entity.guid] = new ConcurrentSet<Client>();
			}

			return entity;
		}

		/// <summary> Revokes an entity by ID </summary>
		/// <param name="guid"> ID of entity to revoke </param>
		/// <returns> True if Entity existed prior and was removed, false otherwise. </returns>
		public bool Revoke(Guid guid) {
			if (entities.ContainsKey(guid)) {
				Log.Debug($"EntityService.Revoke: Master?{isMaster}, revoking entity {guid}");

				Client client = server.GetClient(guid); 
				
				// Remove subscription sets and ensure that everything has been unsubscribed (debugging)
				if (isMaster) {
					ConcurrentSet<Client> subbers;
					
					if (subscribers.TryRemove(guid, out subbers) && subbers.Count > 1) {
						Log.Warning($"EntityService.Revoke: Entity {guid} Had {subbers.Count} remaining subscribers when revoked. Ensure everything has unsubscribed.");
					}

					if (client != null) {

						ConcurrentSet<Guid> subbed;
						if (subscriptions.TryRemove(client, out subbed) && subbed.Count > 1) {
							Log.Warning($"EntityService.Revoke: Client {guid} had {subbed.Count} remaining subscriptions when revoked. Ensure everything has been unsubscribed...");
						}
						

					}
				}

				// Finally, pull the entity and invalidate all WeakReferenceTables for it.
				{ Entity _; entities.TryRemove(guid, out _); }

				if (client != null) {
					// Very last thing done with a client before it is cleaned up:
					client.Finished();
				}
				
				return true;
			}
			return false;
		}

		public override void OnEnable() {
			entities = new ConcurrentDictionary<Guid, Entity>();
			componentTables = new ConcurrentDictionary<Type, ConditionalWeakTable<Entity, Comp>>();
			componentTypes = new ConcurrentDictionary<string, TypeInfo>();
			
			if (isMaster) {
				subscriptions = new ConcurrentDictionary<Client, ConcurrentSet<Guid>>();
				subscribers = new ConcurrentDictionary<Guid, ConcurrentSet<Client>>();
#if !UNITY
				entityInfos = new ConcurrentDictionary<string, EntityInfo>();
				GetService<LoginService>().userInitializer += InitializeEntityInfo;
#endif
			}
		}
		public override void OnDisable() {
			entities = null;
			componentTables = null;
			componentTypes = null;
			if (isMaster) {
#if !UNITY
				entityInfos = null;
				var login = GetService<LoginService>();
				if (login != null) {
					login.userInitializer -= InitializeEntityInfo;
				}
				/// Should have already sent disconnect messages to connected clients 
				subscriptions.Clear();
				subscribers.Clear();
#endif
			}

		}

		/// <summary> Server -> Client RPC. Requests that the client spawn a new entity </summary>
		/// <param name="msg"> RPC Info. </param>
		public void SpawnEntity(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				CreateEntity(id);
				bool islocalEntity = msg.numArgs > 1 && msg[1] == "local";
				Log.Debug($"slave.SpawnEntity: Spawned entity {id}. local? {islocalEntity} ");

				server.On(new EntitySpawned(){ id = id });
				if (islocalEntity) {
					server.On(new SetLocalEntity() { id = id });
				}
			} else {
				Log.Debug($"slave.SpawnEntity: No properly formed guid to spawn.");
			}

		}

		/// <summary> Server -> Client RPC. Requests that the client despawn an existing entity. </summary>
		/// <param name="msg"></param>
		public void DespawnEntity(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			for (int i = 0; i < msg.numArgs; i++) {
				Guid id;
				if (Guid.TryParse(msg[i], out id)) { 
					Revoke(id); 
					server.On(new EntityDespawned(){ id = id });
					Log.Debug($"slave.DespawnEntity: Despawning entity {id}");
				}
			}
				
		}

		/// <summary> Loads a ECS Component type by name. Prepares getters and setters for any value-type data fields </summary>
		/// <param name="name"> Name of type to load </param>
		/// <returns> Type of given ECS component by name, if valid and found. Null otherwise. </returns>
		public Type GetCompType(string name) {
			if (componentTypes.ContainsKey(name)) { return componentTypes[name].type; }
			Type t = Type.GetType(name);
			if (t != null) {
				if (typeof(Comp).IsAssignableFrom(t)) {
					LoadCompType(name, t);
					return t;
				}

				Log.Warning($"Type {name} does not inherit from {typeof(Comp)}.");
				componentTypes[name] = null;

			}
			Log.Warning($"No valid Type {name} could be found");
			componentTypes[name] = null;
			return null;
		}

		/// <summary> Loads required information for interacting with a component of type T. </summary>
		/// <param name="name"> FullName of type </param>
		/// <param name="t"> Type </param>
		private void LoadCompType(string name, Type t) {
			Log.Debug($"EntityService.LoadCompType: isMaster?{isMaster} Loading component {t}");
			TypeInfo info = new TypeInfo();
			info.type = t;
			List<FieldInfo> syncedFields = new List<FieldInfo>();
			List<Delegate> syncedFieldGetters = new List<Delegate>();
			List<Delegate> syncedFieldSetters = new List<Delegate>();
			FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var field in fields) {
				if (field.FieldType.IsValueType) {
					syncedFields.Add(field);

					Func<object, dynamic> getDel = (obj) => { 
						try {
							return field.GetValue(obj); 
						} catch (Exception e) { Log.Warning(e, $"Failed to Get field {t}.{field.Name}"); }
						return null;
					};
					Action<object, dynamic> setDel = (obj, val) => { 
						try {
							field.SetValue(obj, val); 
						} catch (Exception e) { Log.Warning(e, $"Failed to Set field {t}.{field.Name} to a value of type {val.GetType()} "); }
					};
					syncedFieldGetters.Add(getDel);
					syncedFieldSetters.Add(setDel);
					
				}
			}
			info.syncedFields = syncedFields.ToArray();
			info.syncedFieldGetters = syncedFieldGetters.ToArray();
			info.syncedFieldSetters = syncedFieldSetters.ToArray();

			componentTypes[name] = info;
		}

		/// <summary> Server -> Client RPC. Requests the client add components to an entity </summary>
		/// <param name="info"> RPC Info </param>
		public void AddComps(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				Log.Debug($"slave.AddComps: Adding {msg.numArgs-1} Comps to entity {id}");
				for (int i = 1; i < msg.numArgs; i++) {
					string typeName = msg[i];
					Type type = GetCompType(typeName);
					AddComponent(id, type);
					server.On(new ComponentAdded() { id = id, componentType = type });
				}
			}
		}

		/// <summary> Server -> Client RPC. Requests the client removes components from an entity </summary>
		/// <param name="info"> RPC Info </param>
		public void RemoveComps(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }

			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				Log.Debug($"slave.AddComps: Removing {msg.numArgs - 1} Comps to entity {id}");

				for (int i = 1; i < msg.numArgs; i++) {
					string typeName = msg[i];
					Type type = GetCompType(typeName);
					RemoveComponent(id, type); 
					server.On(new ComponentRemoved() { id = id, componentType = type });

				}
			}
		}

		/// @BAD @HACKY @IMPROVEME - Baking+caching generic args for packers/unpackers. And other stuff.
		/// There has gotta be a more efficient way to bind generic types to unknown function calls.
		/// Maybe bake lambdas instead? I know those can leak captures....
		private static MethodInfo PACKER;
		private static MethodInfo UNPACKER;
		private static ConcurrentDictionary<Type, MethodInfo> GENERIC_PACKERS;
		private static ConcurrentDictionary<Type, MethodInfo> GENERIC_UNPACKERS;
		private static ConcurrentDictionary<Type, bool> SERVER_ONLY;
		private static ConcurrentDictionary<Type, bool> OWNER_ONLY;
		/// <summary> Get (bake if not present) data packer MethodInfo </summary>
		public static MethodInfo GET_PACKER(Type t) {
			if (!t.IsValueType) { return null; }
			if (PACKER == null) { INITIALIZE_CACHES(); }
			if (!GENERIC_PACKERS.ContainsKey(t)) { GENERIC_PACKERS[t] = PACKER.MakeGenericMethod(t); }
			return GENERIC_PACKERS[t];
		}
		/// <summary> Get (bake if not present) data unpacker MethodInfo </summary>
		public static MethodInfo GET_UNPACKER(Type t) {
			if (!t.IsValueType) { return null; }
			if (PACKER == null) { INITIALIZE_CACHES(); }
			if (!GENERIC_UNPACKERS.ContainsKey(t)) { GENERIC_UNPACKERS[t] = UNPACKER.MakeGenericMethod(t); }
			return GENERIC_UNPACKERS[t];
		}
		/// <summary> Get wether or not a component should be synced, based on decoration, and it's status as being owned by the client. </summary>
		/// <param name="component"> Component to query for </param>
		/// <param name="clientOwns"> True if the client owns this component, false if it belongs to something else </param>
		/// <returns> True, if the component should be synced, otherwise false. </returns>
		public static bool IS_SYNCED(Comp component, bool clientOwns) {
			Type t = component.GetType();
			bool serverOnly = IS_SERVER_ONLY(t);
			bool ownerOnly = IS_OWNER_ONLY(t);
			
			if (clientOwns) { return !serverOnly; } 
			return !serverOnly && !ownerOnly;
		}
		/// <summary> Get (bake if not present) server-only status for a given component </summary>
		public static bool IS_SERVER_ONLY(Comp component) { return IS_SERVER_ONLY(component.GetType()); }
		/// <summary> Get (bake if not present) server-only status for a given component type </summary>
		public static bool IS_SERVER_ONLY(Type t) {
			if (SERVER_ONLY == null) { INITIALIZE_CACHES(); }
			if (!SERVER_ONLY.ContainsKey(t)) {
				SERVER_ONLY[t] = t.GetCustomAttribute<ServerOnlyComponentAttribute>() != null;
			}
			return SERVER_ONLY[t];
		}
		/// <summary> Get (bake if not present) owner-only status for a given component</summary>
		public static bool IS_OWNER_ONLY(Comp component) { return IS_OWNER_ONLY(component.GetType()); }
		/// <summary> Get (bake if not present) owner-only status for a given component type </summary>
		public static bool IS_OWNER_ONLY(Type t) {
			if (OWNER_ONLY== null) { INITIALIZE_CACHES(); }
			if (!OWNER_ONLY.ContainsKey(t)) {
				OWNER_ONLY[t] = t.GetCustomAttribute<OwnerOnlySyncAttribute>() != null;
			}
			return OWNER_ONLY[t];
		}
		private static void INITIALIZE_CACHES() {
			PACKER = typeof(Pack).GetMethod("Base64", BindingFlags.Static | BindingFlags.Public);
			UNPACKER = typeof(Unpack).GetMethod("Base64", BindingFlags.Static | BindingFlags.Public);
			GENERIC_PACKERS = new ConcurrentDictionary<Type, MethodInfo>();
			GENERIC_UNPACKERS = new ConcurrentDictionary<Type, MethodInfo>();
			SERVER_ONLY = new ConcurrentDictionary<Type, bool>();
			OWNER_ONLY = new ConcurrentDictionary<Type, bool>();
		}

		/// <summary> Server -> Client RPC. Requests the client set information into a component. </summary>
		/// <param name="msg"></param>
		public void SetComponentInfo(RPCMessage msg) {
			/// noop on server
			if (isMaster) { return; }
			Guid id;
			if (Guid.TryParse(msg[0], out id)) {
				string typeName = msg[1];
				Type type = GetCompType(typeName);
				TypeInfo info = componentTypes[typeName];

				Log.Debug($"slave.SetComponentInfo: Setting info for {id}.{typeName}, {msg.numArgs-2} fields.");
				Comp component = GetComponent(id, type);
				TypedReference cref = __makeref(component);
				
				if (component != null) {
					if (component.lastServerModification < msg.sentAt) {
						component.lastServerModification = msg.sentAt;

						Log.Debug($"slave.SetComponentInfo:\nBefore: {component}");
						object[] unpackerArgs = new object[1];
						for (int i = 0; i+2 < msg.numArgs && i < info.syncedFields.Length; i++) {
							FieldInfo field = info.syncedFields[i];
							unpackerArgs[0] = msg[i + 2];
							try {
							
								field.SetValue(component, GET_UNPACKER(field.FieldType).Invoke(null, unpackerArgs));
								//This doesn't work inside of unity because mono.
								//field.SetValueDirect(cref, GET_UNPACKER(field.FieldType).Invoke(null, unpackerArgs));
							} catch (Exception e) {
								Log.Warning(e, $"Failed to unpack and set {field.FieldType} {type}.{field.Name}");
							}
						}
						Log.Debug($"slave.SetComponentInfo:\nAfter: {component}");

						server.On(new ComponentChanged() { id = id, componentType = type });
					} else {
						Log.Debug($"slave.SetComponentInfo:\nComponent was more recently modified.");
					}
				} else {

					Log.Debug($"slave.SetComponentInfo: No COMPONENT {type} FOUND on {id}! ");
				}

			}
		}

		/// <summary> Makes the client be subscribed to all messages for the given entity by id </summary>
		/// <param name="client"> Client to subscribe </param>
		/// <param name="id"> ID of entity to subscribe to </param>
		public void Subscribe(Client client, Guid id) {
			if (isMaster) {
				Entity entity = this[id];
				if (entity == null) { 
					Log.Debug($"Master: no entity for {id} to subscribe {client.identity} to");
					return; 
				}

				var subsA = subscribers[id];
				var subsB = subscriptions[client];
			
				if (!subsA.Contains(client) && !subsB.Contains(id)) {
					Log.Debug($"Master: Subscribing {client.identity} to {id}");
					subsA.Add(client);
					subsB.Add(id);
					if (id == client.id) {
						client.Call(SpawnEntity, id, "local");
					} else { 
						client.Call(SpawnEntity, id);
					}

					// Todo: Clean this up and separate into deltas
					Comp[] components = GetComponents(id);
					List<object> addArgs = new List<object>();
					addArgs.Add(id);
					// For now, the only components that are considered owned by a client
					// are ones on the entity that is the client's own id
					// TODO: If we need, add a component type for owning by proxy
					//		check for the component on the object, and check if the id matches.
					bool clientOwns = client.id == id;
					foreach (var component in components) {
						// only add entities that should be sync'd
						if (IS_SYNCED(component, clientOwns)) {
							addArgs.Add(component.GetType().FullName); 
						}
					}

					client.Call(AddComps, addArgs.ToArray());
					
					List<object[]> argLists = PackSetComponentArgs(id, components, clientOwns);
					foreach (var args in argLists) {
						client.Call(SetComponentInfo, args);
					}
					
				} else {
					Log.Debug($"Master: {client.id} already subscribed to {id}");
				}
			}
		}

		/// <summary> Removes an entity ID from a client's subscription list, if they were subscribed. </summary>
		/// <param name="client"> Client to unsubscribe </param>
		/// <param name="id"> ID to unsub from </param>
		public void Unsubscribe(Client client, Guid id) {
			if (isMaster) {
				Entity entity = this[id];
				if (entity == null) {
					Log.Debug($"Master: no entity for {id} to unsubscribe {client.identity} from");
					return;
				}
				if (id == client.id) {
					Log.Debug($"Master: Cannot unsub client from its own entity");
					return;
				}
				

				var subsA = subscribers[id];
				var subsB = subscriptions[client]; 

				if (subsA.Contains(client) && subsB.Contains(id)) {
					Log.Debug($"Master: Unsubbing {client.id} from {id}");
					subsA.Remove(client);
					subsB.Remove(id);

					if (!client.closed) { client.Call(DespawnEntity, id); }

				}


			}
		}

		/// <summary> Pack all of the args for all components on the given entity for synchronization </summary>
		/// <param name="id"> ID of entity to sync </param>
		/// <param name="clientOwns"> true if the client these args are being generated for is an owner of the entity </param>
		/// <returns> List of arguments to send to the client </returns>
		private List<object[]> PackSetComponentArgs(Guid id, bool clientOwns) {
			var components = GetComponents(id);
			return PackSetComponentArgs(id, components, clientOwns);
		}

		/// <summary> Pack all of the args for all given components for synchronization </summary>
		/// <param name="id"> ID of entity components belong to </param>
		/// <param name="components"> Components to pack </param>
		/// <param name="clientOwns"> If the client in question owns this entity </param>
		/// <returns> List of arguments to send to client to sync all components </returns>
		private List<object[]> PackSetComponentArgs(Guid id, Comp[] components, bool clientOwns) {
			List<object[]> argLists = new List<object[]>();
			
			foreach (var component in components) {
				if (IS_SYNCED(component, clientOwns)) {
					argLists.Add(PackSetComponentArgs(id, component));
				}
			}

			return argLists;
		}
		/// <summary> Pack the args to set component data on the client </summary>
		/// <param name="id"> ID of entity component belongs to </param>
		/// <param name="component"> Component to sync </param>
		/// <returns> object[] of args to be sent to a client to sync that component's data </returns>
		private object[] PackSetComponentArgs(Guid id, Comp component) {
			List<object> args = new List<object>();
			args.Add(id);
			string typeName = component.GetType().FullName;
			args.Add(typeName);
			var type = GetCompType(typeName);
			var typeInfo = componentTypes[typeName];

			for (int i = 0; i < typeInfo.syncedFields.Length; i++) {
				dynamic value = typeInfo.syncedFieldGetters[i].DynamicInvoke(component);
				args.Add(Pack.Base64(value));
			}

			return args.ToArray();
		}


		public override void OnConnected(Client client) {
			if (isMaster) {
				Entity entity = CreateEntity(client.id);
				subscriptions[client] = new ConcurrentSet<Guid>();

				Log.Info($"OnConnected for {client.id}, entity created");

				Subscribe(client, client.id);
			} else {
				Log.Info($"Clientside OnConnected.");
			}
		}

#if !UNITY
		public override void OnDisconnected(Client client) {
			if (isMaster) {
				Log.Debug($"EntityService.OnDisconnected: \\oIsMaster: {isMaster} Cleaning up entity for client {client.identity}. ");
				Entity entity = entities.ContainsKey(client.id) ? entities[client.id] : null;

				TRS trs = GetComponent<TRS>(entity);
				OnMap onMap = GetComponent<OnMap>(entity);

				var db = GetService<DBService>();
				var loginService = GetService<LoginService>();

				LoginService.Session? session = loginService.GetLogin(client);
				Credentials creds;
				if (session.HasValue) {
					creds = session.Value.credentials;
					Log.Verbose($"EntityService.OnDisconnected: Getting entity for client {client.identity}, id={creds.userId}/{creds.username}");

					var info = db.Get<UserEntityInfo>(creds.userId);
					if (info == null) {
						Log.Error($"EntityService.OnDisconnected: Problem, no EntityInfo was initialized for {creds.userId}");
					}
					if (onMap != null) {
						
						info.map = onMap.mapId;
					}
					
					Log.Debug($"EntityService.OnDisconnected: Saving entity data for {creds.username} ");
					db.Save(info);
					

				} else {
				
					Log.Verbose($"EntityService.OnDisconnected: No login session for {client.identity}, skipping saving entity data.");
				}

				
			} else {

				Log.Info($"EntityService.OnDisconnected: Clientside OnDisconnected.");
			}
		}

		public override void OnFinishedDisconnected(Client client) {
			// Only after all other potentially entity handling things have handled the entity should it be revoked.
			// This method runs after all OnDisconnected() have run for a client.
			
			// Get entity for client
			Entity e = this[client.id];

			if (e != null) {

				OnMap onMap = e.GetComponent<OnMap>();
				if (onMap != null) {
					// If they are on a map, delay revocation until map is ready.
					Log.Debug($"\\eRemoving {client.identity} from {onMap.mapId}:{onMap.mapInstanceIndex}");
					GetService<MapService>().ExitMap(client, onMap.mapId, onMap.mapInstanceIndex);
				} else {
					// Otherwise revoke it here and now.
					// This happens for example, if they do not log in.
					Revoke(client.id);
				}

			}
		}

		/// <summary> Called when a login occurs. </summary>
		/// <param name="succ"></param>
		public void On(LoginService.LoginSuccess_Server succ) {
			if (!isMaster) { return; }
			Log.Info("EntityService.On(LoginSuccess_Server)");

			Client client = succ.client;
			Guid clientId = client.id;
			Log.Info($"{nameof(EntityService)}: Got LoginSuccess for {succ.client.identity} !");
			var user = GetService<LoginService>().GetLogin(client);
			Guid userId = user.HasValue ? user.Value.credentials.userId : Guid.Empty;
			string username = user.HasValue ? user.Value.credentials.username : "[NoUser]";

			var db = GetService<DBService>();
			UserEntityInfo info = db.Get<UserEntityInfo>(userId);
			var trs = AddComponent<TRS>(clientId);
			var nameplate = AddComponent<Nameplate>(clientId);

			//{ // Testing: Add some data and see if it is synced/hidden properly
			//	var hidden = AddComponent<SomeHiddenData>(clientId);
			//	var secret = AddComponent<SomeSecretData>(clientId);
			//	hidden.key = 123456789;
			//	secret.key = 987654321;
			//	hidden.Send(); // Gotta remember to send component data to clients, even if it may be hidden.
			//	secret.Send();
			//}

			Log.Info($"OnLoginSuccess_Server for user {clientId} -> { username } / UserID={userId }, EntityInfo={info}, TRS={trs}");
			nameplate.name = username;
			nameplate.Send();

			
			if (info != null) {

				trs.position = info.position;
				trs.rotation = info.rotation;
				trs.scale = Vector3.one;

				trs.Send();

				GetService<MapService>().EnterMap(client, info.map, info.position, info.rotation);
			
			} else {
					
			}
		}
#endif

		/// <summary> Gets the ConditionalWeakTable for a given entity type. </summary>
		/// <typeparam name="T"> Generic type of table to get </typeparam>
		/// <returns> ConditionalWeakTable mapping entities to Components of type T </returns>
		private ConditionalWeakTable<Entity, Comp> GetTable<T>() {
			Type type = typeof(T);
			return GetTable(type);
		}

		/// <summary> Gets the ConditionalWeakTable for a given entity type. </summary>
		/// <param name="type"> Type of table to get </typeparam>
		/// <returns> ConditionalWeakTable mapping entities to Components of type T </returns>
		private ConditionalWeakTable<Entity, Comp> GetTable(Type type) {
			return !componentTables.ContainsKey(type)
							? (componentTables[type] = new ConditionalWeakTable<Entity, Comp>())
							: componentTables[type];
		}


		/// <summary> Adds a component of type T for the given entity. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to add Component to </param>
		/// <returns> Newly created component </returns>
		public T AddComponent<T>(Guid id) where T : Comp { return (T) AddComponent(this[id], typeof(T)); }

		/// <summary> Adds a component of type T for the given entity. </summary>
		/// <param name="t"> Type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to add Component to </param>
		/// <returns> Newly created component </returns>
		public Comp AddComponent(Guid id, Type t) {
			bool serverOnly = IS_SERVER_ONLY(t);
			bool ownerOnly = IS_OWNER_ONLY(t);

			Entity entity = this[id];
			if (!typeof(Comp).IsAssignableFrom(t)) { throw new Exception($"{t} is not a valid ECS Component type."); }
			var table = GetTable(t);

			Comp check;
			if (table.TryGetValue(entity, out check)) {
				throw new InvalidOperationException($"Entity {entity.guid} already has a component of type {t}!");
			}

			Comp component = (Comp)Activator.CreateInstance(t);
			component.Bind(entity);
			table.Add(entity, component);
			if (isMaster) {
				var subs = subscribers[id];
				if (!serverOnly) {
					string addMsg = Client.FormatCall(AddComps, id, t.FullName);

					foreach (var client in subs) {

						// For now, the only components that are considered owned by a client
						// are ones on the entity that is the client's own id
						// TODO: If we need, add a component type for owning by proxy
						//		check for the component on the object, and check if the id matches.
						bool clientOwns = client.id == id;
						if (!ownerOnly || clientOwns) {
							client.SendTCPMessageDirectly(addMsg); 
						}
					}

				}
				/// Actually don't need to send data yet, components will be empty right when added.
				//var args = PackSetComponentArgs(id, component);
				//string setMsg = Client.FormatCall(SetComponentInfo, args);
				//foreach (var client in subs) { client.SendMessageDirectly(setMsg); }
			}

			return component;
		}

		/// <summary> Gets a component for the given entity </summary>
		/// <typeparam name="T"> Generic type of Component to get </typeparam>
		/// <param name="id"> ID of Entity to get Componment from </param>
		/// <returns> Component of type T if it exists on entity, otherwise null. </returns>
		public T GetComponent<T>(Guid id) where T : Comp { return (T)GetComponent(this[id], typeof(T)); }

		/// <summary> Gets a component for the given entity </summary>
		/// <param name="t"> Type of Component to get </typeparam>
		/// <param name="entity"> Entity to get Componment from </param>
		/// <returns> Component of type T if it exists on entity, otherwise null. </returns>
		public Comp GetComponent(Guid id, Type t) {
			Entity entity = this[id];
			if (entity == null) { return null; }
			var table = GetTable(t);

			Comp c;
			if (table.TryGetValue(entity, out c)) { return (Comp) c; }

			return null;
		}

		/// <summary> Gets all associated components with an entity by a given id </summary>
		/// <param name="id"> ID of entity to check </param>
		/// <returns> Array of Components associated with the given Entity id </returns>
		/// <remarks> as with <see cref="Comp"/>, do NOT hold onto references to the array. </remarks>
		public Comp[] GetComponents(Guid id) {
			Entity e = this[id];
			if (e == null) { return new Comp[0]; }

			List<Comp> components = new List<Comp>();
			foreach (var pair in componentTables) {
				Type type = pair.Key;
				var table = pair.Value;
				Comp comp;
				if (table.TryGetValue(e, out comp)) { components.Add(comp); }
			}
			return components.ToArray();
		}

		/// <summary> Checks the entity for a given component type, and if it exists, returns it, otherwise adds one and returns it. </summary>
		/// <typeparam name="T"> Generic type of Component to add </typeparam>
		/// <param name="entity"> Entity to check and/or add Component to </param>
		/// <returns> Previously existing or newly created component </returns>
		public T RequireComponent<T>(Guid id) where T : Comp { return (T) RequireComponent(id, typeof(T)); }

		/// <summary> Checks the entity for a given component type, and if it exists, returns it, otherwise adds one and returns it. </summary>
		/// <param name="t"> Type of Component to add </typeparam>
		/// <param name="id"> ID of Entity to check and/or add Component to </param>
		/// <returns> Previously existing or newly created component </returns>
		public Comp RequireComponent(Guid id, Type t) {
			Entity entity = this[id];
			if (entity == null) { return null; }
			var c = GetComponent(id, t);
			if (c != null) { return c; }
			return AddComponent(id, t); 
		}

		/// <summary> Removes a component from the given entity  </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <param name="id"> ID of Entity to remove component from </param>
		/// <returns> True if component existed prior  and was removed, false otherwise. </returns>
		public bool RemoveComponent<T>(Guid id) where T : Comp { return RemoveComponent(id, typeof(T)); }

		/// <summary> Removes a component from the given entity  </summary>
		/// <typeparam name="T"> Generic type of Component to remove </typeparam>
		/// <param name="entity"> Entity to remove component from </param>
		/// <returns> True if component existed prior  and was removed, false otherwise. </returns>
		public bool RemoveComponent(Guid id, Type t) {
			bool serverOnly = IS_SERVER_ONLY(t);
			bool ownerOnly = IS_OWNER_ONLY(t);
			Entity entity = this[id];
			if (entity == null) { return false; }
			var table = GetTable(t);

			Comp c;
			if (table.TryGetValue(entity, out c)) { c.Invalidate(); }
			if (isMaster) {
				var subs = subscribers[id];
				// Only send non-server only components...
				if (!serverOnly) {
					foreach (var client in subs) {
						// For now, the only components that are considered owned by a client
						// are ones on the entity that is the client's own id
						// TODO: If we need, add a component type for owning by proxy
						//		check for the component on the object, and check if the id matches.
						bool clientOwns = client.id == id;
						if (!ownerOnly || clientOwns) {
							client.Call(RemoveComps, id, t.FullName); 
						}
					}

				}
				
			}

			return table.Remove(entity);
		}

		/// <summary> Sends the information for a component to all subscribers of that component's entity </summary>
		/// <param name="comp"> Component to send </param>
		public void SendComponent(Comp comp) {
			Type t = comp.GetType();
			bool serverOnly = IS_SERVER_ONLY(t);
			if (serverOnly) { return; }
			bool ownerOnly = IS_OWNER_ONLY(t);

			Guid id = comp.entityId;
			var subs = subscribers[id];
			var args = PackSetComponentArgs(id, comp);

			foreach (var sub in subs) {
				// For now, the only components that are considered owned by a client
				// are ones on the entity that is the client's own id
				// TODO: If we need, add a component type for owning by proxy
				//		check for the component on the object, and check if the id matches.
				bool clientOwns = sub.id == id;

				if (!ownerOnly || clientOwns) {
					sub.Call(SetComponentInfo, args);
				}
			}
		}

		/// <summary> Get a snapshot list of all entities that have the given component type attached. </summary>
		/// <typeparam name="T"> Component type to search for </typeparam>
		/// <param name="lim"> List of Guids to check </param>
		/// <returns> A list of Entities for the given guids that exist and have the given component associated with them </returns>
		public List<Entity> GetEntities<T>(IEnumerable<Guid> lim = null) where T : Comp {
			if (lim == null) { lim = entities.Keys; }
			List<Entity> ents = new List<Entity>();
			
			var table = GetTable<Entity>();
			Comp it;
			foreach (var guid in lim) {
				Entity e = this[guid];
				if (e != null && table.TryGetValue(e, out it)) {
					ents.Add(e);
				}
			}

			return ents;
		}

		/// <summary> Get a list of all entities that have the given component types attached. </summary>
		/// <typeparam name="T1"> Component type to search for </typeparam>
		/// <typeparam name="T2"> Component type to search for </typeparam>
		/// <param name="lim"> List of Guids to check </param>
		/// <returns> A list of Entities for the given guids that exist and have the given components associated with them </returns>
		public List<Entity> GetEntities<T1, T2>(IEnumerable<Guid> lim = null) where T1 : Comp where T2 : Comp {
			if (lim == null) { lim = entities.Keys; }
			List<Entity> ents = new List<Entity>();

			var table1 = GetTable<T1>();
			var table2 = GetTable<T2>();
			Comp it1, it2;
			foreach (var guid in lim) {
				Entity e = this[guid];
				if (e != null
					&& table1.TryGetValue(e, out it1) 
					&& table2.TryGetValue(e, out it2)) {
					ents.Add(e);
				}
			}

			return ents;
		}

		/// <summary> Get a list of all entities that have the given component types attached. </summary>
		/// <typeparam name="T1"> Component type to search for </typeparam>
		/// <typeparam name="T2"> Component type to search for </typeparam>
		/// <typeparam name="T3"> Component type to search for </typeparam>
		/// <param name="lim"> List of Guids to check </param>
		/// <returns> A list of Entities for the given guids that exist and have the given components associated with them </returns>
		public List<Entity> GetEntities<T1, T2, T3>(IEnumerable<Guid> lim = null) where T1 : Comp where T2 : Comp where T3 : Comp {
			if (lim == null) { lim = entities.Keys; }
			List<Entity> ents = new List<Entity>();

			var table1 = GetTable<T1>();
			var table2 = GetTable<T2>();
			var table3 = GetTable<T3>();
			Comp it1, it2, it3;
			foreach (var guid in lim) {
				Entity e = this[guid];
				if (e != null 
					&& table1.TryGetValue(e, out it1)
					&& table2.TryGetValue(e, out it2)
					&& table3.TryGetValue(e, out it3)) {
					ents.Add(e);
				}
			}

			return ents;
		}

	}
	
}
