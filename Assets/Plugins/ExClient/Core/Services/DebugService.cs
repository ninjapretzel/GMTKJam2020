using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Service providing network debugging messages when common events occur. </summary>
	public class DebugService : Service {
		public override void OnEnable() {
			Log.Verbose("Debug Service Enabled");
		}

		public override void OnDisable() {
			Log.Verbose("Debug Service Disabled");
		}

		public float timeout;
		public bool enableDebugPings = false;
		public override void OnTick(float delta) {
			// Log.Verbose("Debug service tick");
			if (!isMaster && enableDebugPings) {
				timeout += delta;
				if (timeout > 1.0f) {
					server.localClient.Hurl(this.Ping);
					timeout -= 1.0f;
				}
			}
		}


		public override void OnConnected(Client client) {
			Log.Verbose($"Connected {client.identity}");
		}

		public override void OnDisconnected(Client client) {
			Log.Verbose($"Disconnected {client.identity}");
		}

		public void Ping(RPCMessage msg) {
			Log.Info($"Ping'd by {msg.sender.identity}");

			// Since we are an instance, we can reference the Pong method directly. 
			if (msg.wasUDP) {
				msg.sender.Hurl(Pong);
			} else {
				msg.sender.Call(Pong);
			}

			// If accessing another service's methods, this will help keep references during refactoring:
			// sender.Call(Members<DebugService>.i.Pong);

		}
		public void Pong(RPCMessage msg) {

			Log.Info($"Pong'd by {msg.sender.identity}");

		}

		
	}
	
}
