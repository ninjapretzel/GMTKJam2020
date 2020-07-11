using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Ex;
using LevelUpper.Markdown;
using System.Collections.Concurrent;
using System.Threading;

public class ExDaemon : MonoBehaviour {

	public string targetHost = "localhost";
	public int targetPort = 32055;
	public Client client;

	public float time = 0;
	private ConcurrentQueue<Action> toRun = new ConcurrentQueue<Action>();
	public void RunOnMainThread(Action action) { toRun.Enqueue(action); }
	
	void Awake() {
		
	}
	
	void Start() {
		Thread connector = new Thread(Connect);
		connector.Start();

	}
	
	void Update() {
		
		Action action;
		while (toRun.TryDequeue(out action)) {
			try {
				action();
			} catch (Exception e) {
				Debug.LogWarning($"Error running function on main thread {e.InfoString()}");
			}
		}


		

	}


	Ex.Logger logger = (tag, msg) => {
		Debug.unityLogger.Log(tag, msg.ReplaceMarkdown());
	};
	void OnEnable() {
		Log.LEVEL_CODES[(int)LogLevel.Info] = "\\k";

		Log.logHandler += logger;
	}
	void OnDisable() {
		Debug.Log("Disconnecting.");
		client?.DisconnectSlave();
		Log.logHandler -= logger;
	}

	public void Connect() {
		while (true) {
			
			TcpClient tcp = null;
			try {
				tcp = new TcpClient(targetHost, targetPort);
				Client client = new Client(tcp);
		
				client.AddService<DebugService>();
				client.AddService<LoginService>();
				client.AddService<EntityService>();
				client.AddService<MapService>();
				client.AddService<SyncService>();


				//*
				// client.AddService<ExPlayerLink.>().Bind(this);
				client.AddService<ExEntityLink.ExEntityLinkService>().Bind(this);
				ExEntityLink.AutoRegisterComponentChangeCallbacks();
				// ExEntityLink.Register<Ex.Terrain>(TerrainGenerator)
				//*/

				client.ConnectSlave();
				this.client = client;
				RunOnMainThread(() => {
					BroadcastMessage("OnExConnected", SendMessageOptions.DontRequireReceiver);
				});

			} catch (Exception e) {
				Debug.LogWarning($"Error on connection: {e}");
				if (tcp != null) {
					tcp.Dispose();
				}
			}
			break;
		}
	}
	
}


