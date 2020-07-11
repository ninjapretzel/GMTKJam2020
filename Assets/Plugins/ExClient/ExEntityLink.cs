using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Ex;
using System.Reflection;

public class ExEntityLink : MonoBehaviour {

	/// <summary> Attribute used to search for callbacks for automatic registration </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class AutoRegisterChangeAttribute : Attribute {}

	/// <summary> Attribute used to search for callbacks for automatic registration </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class AutoRegisterRemoveAttribute : Attribute {}

	/// <summary> Delegate for callbacks when data is recieved from server </summary>
	/// <typeparam name="T"> Type of Network Component </typeparam>
	/// <param name="t"> Instance of Network Component </param>
	/// <param name="link"> Linked unity entity </param>
	public delegate void OnChange<T>(T t, ExEntityLink link) where T : Comp;
	/// <summary> Delegate for callbacks when a component is going to be removed from an entity by the server </summary>
	/// <typeparam name="T"> Type of Network Component </typeparam>
	/// <param name="t"> Instance of Network Component </param>
	/// <param name="link"> Linked unity entity </param>
	public delegate void OnRemove<T>(T t, ExEntityLink link) where T : Comp;
	
	/// <summary> Search loaded assemblies for callbacks to automatically regester changes for </summary>
	public static void AutoRegisterComponentChangeCallbacks() {
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		
		foreach (var assembly in assemblies) {
			/// These are almost always impossible to inspect. 
			if (assembly.FullName.Contains("Microsoft.GeneratedCode")) { continue; }

			try {
				Type[] types = assembly.GetExportedTypes();

				foreach (var type in types) {
					MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
					foreach (var method in methods) {
						var param = method.GetParameters();
						if (method.GetCustomAttribute<AutoRegisterChangeAttribute>() != null) {

							if (param.Length == 2
									&& typeof(Comp).IsAssignableFrom(param[0].ParameterType)
									&& typeof(ExEntityLink).IsAssignableFrom(param[1].ParameterType)) {

								Type paramType = param[0].ParameterType;

								changes[paramType] = method.CreateDelegate(
									typeof(OnChange<>)
									.GetGenericTypeDefinition()
									.MakeGenericType(paramType));

							}
						} else if (method.GetCustomAttribute<AutoRegisterRemoveAttribute>() != null) {
							if (param.Length == 2
									&& typeof(Comp).IsAssignableFrom(param[0].ParameterType)
									&& typeof(ExEntityLink).IsAssignableFrom(param[1].ParameterType)) {
								
								Type paramType = param[0].ParameterType;

								removals[paramType] = method.CreateDelegate(
									typeof(OnRemove<>)
									.GetGenericTypeDefinition()
									.MakeGenericType(paramType));

							}
						}


					}

				}

			} catch (Exception e) {
				Debug.LogWarning($"Could not inspect assembly {assembly.FullName}:{e}");
			}
		}

	}


	/// <summary> Register an <see cref="OnChange{T}"/>callback for a type. Replaces the existing callback with the new one. </summary>
	/// <typeparam name="T"> Generic network component type to add to callbacks </typeparam>
	/// <param name="onChange"> Callback </param>
	public static void RegisterOnChange<T>(OnChange<T> onChange) where T : Comp { changes[typeof(T)] = onChange; }
	/// <summary> Removes an <see cref="OnChange{T}"/> callback </summary>
	/// <typeparam name="T"> Generic type to remove from callbacks  </typeparam>
	public static void UnregisterOnChange<T>() where T : Comp { changes[typeof(T)] = null; }

	/// <summary> Register an <see cref="OnRemove{T}"/>callback for a type. Replaces the existing callback with the new one. </summary>
	/// <typeparam name="T"> Generic network component type to add to callbacks </typeparam>
	/// <param name="onChange"> Callback </param>
	public static void RegisterOnRemove<T>(OnRemove<T> onRemove) where T : Comp { removals[typeof(T)] = onRemove; }
	/// <summary> Removes an <see cref="OnRemove{T}"/> callback </summary>
	/// <typeparam name="T"> Generic type to remove from callbacks  </typeparam>
	public static void UnregisterOnRemove<T>() where T : Comp { removals[typeof(T)] = null; }
	
	/// <summary> Service used to talk to server </summary>
	public class ExEntityLinkService : Ex.Service {
		private ExDaemon daemon;
		EntityService entityService { get { return daemon != null ? daemon.client?.GetService<EntityService>() : null;} }
		public void Bind(ExDaemon daemon) { this.daemon = daemon; }

		public void On(MapService.RubberBand_Client rubber) {
			daemon.RunOnMainThread(()=> {
				Guid id = rubber.id;
				Vector3 pos = rubber.pos;
				Vector3 rot = rubber.rot;
				Debug.Log($"Need to rubberband {id} to {pos}/{rot}");

				if (links.ContainsKey(id)) {
					var link = links[id];
					link.transform.position = pos;
					link.transform.rotation = Quaternion.Euler(rot);
				}

			});
		}

		public void On(EntityService.EntitySpawned spawn) {
			daemon.RunOnMainThread(()=> {
				Guid id = spawn.id;
				GameObject gob = new GameObject(""+id);
				ExEntityLink link = gob.AddComponent<ExEntityLink>();
				link.id = id;
				link.daemon = daemon;
				links[id] = link;

				
			});
		}

		public void On(EntityService.EntityDespawned spawn) {
			daemon.RunOnMainThread(()=>{ 
				Guid id = spawn.id;
				GameObject gob = links[id].gameObject;
				Destroy(gob);
				links[id] = null;
			});
		}

		public void On(EntityService.SetLocalEntity local) {
			daemon.RunOnMainThread(() => {
				Guid id = local.id;
				var playerLink = links[id].gameObject.AddComponent<ExPlayerLink>();
				playerLink.service = this;
			});
		}


		public void On(EntityService.ComponentChanged change) {
			daemon.RunOnMainThread(() => {
				Guid id = change.id;
				Type type = change.componentType;

				if (links.ContainsKey(id) && changes.ContainsKey(type)) {
					var link = links[id];
					Comp comp = entityService.GetComponent(id, type);
					
					try {
						var del = changes[type];
						if (link == null) { Debug.LogWarning($"Tried to dynamic invoke {del.Method} on null <Link>!"); }
						else if (comp == null) { Debug.LogWarning($"Tried to dynamic invoke {del.Method} on null <Comp>!"); }
						else { del.DynamicInvoke(new object[] { comp, link }); }
						
					} catch (Exception e) {
						Debug.LogWarning($"Exception during component data changed for {id}#{type}\n{e}");
					}
				}

			});
		}

		public void On(EntityService.ComponentRemoved removal) {
			daemon.RunOnMainThread(() => {
				Guid id = removal.id;
				Type type = removal.componentType;

				if (links.ContainsKey(id) && removals.ContainsKey(type)) {
					var link = links[id];
					Comp comp = entityService.GetComponent(id, type);
					try {

						var del = removals[type];
						del.DynamicInvoke(new object[] { comp, link });

					} catch (Exception e) {
						Debug.LogWarning($"Exception during component removed for {id}#{type}\n{e}");
					}

				}
			});
		}



	}

	public static Dictionary<Type, Delegate> changes = new Dictionary<Type, Delegate>();
	public static Dictionary<Type, Delegate> removals = new Dictionary<Type, Delegate>();
	public static Dictionary<Guid, ExEntityLink> links = new Dictionary<Guid, ExEntityLink>();
	public static bool loaded = Load();
	private static bool Load() {
		RegisterOnChange<TRS>((trs, link) => {
			var t = link.transform;
			Debug.Log($"Moving {link.id} TRS to {trs.position}, {trs.rotation}");
			t.position = trs.position;
			t.rotation = Quaternion.Euler(trs.rotation);
			t.localScale = trs.scale;
		});
		return true;
	}
	public Guid id;
	public ExDaemon daemon;
	EntityService entityService {
		get {
			return daemon?.client?.GetService<EntityService>();
		}
	}
	
}
