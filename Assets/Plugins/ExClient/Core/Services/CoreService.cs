using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	/// <summary> Service that includes core functionality. Always on a server by default. </summary>
	public class CoreService : Service {

		public override void OnConnected(Client client) {
			// Log.Info($"Core Service connected {client.identity}");
			if (server.isSlave) {
				// Log.Info($"Slave beginning syn-synack-ack process {client.identity}");
				// client.Call(Syn, client.id);
			}
		}

		public override void OnDisconnected(Client client) {
			if (server.isMaster) {
				
				if (!client.closed) {
					client.Call(Closed);
					server.SendData(client);
				}

			}
		}

		/// <summary> RPC sent by a client when it explicitly disconnects. </summary>
		public void Closed(RPCMessage msg) {
			Log.Debug($"Closing Client {msg.sender.identity} isSlave?{msg.sender.isSlave}");
			if (server.isMaster) {
				// Single client was closed remotely.
				server.Close(msg.sender);
			} else {
				// Remote server was closed.
				server.Stop();
			}
		}

		/// <summary> Testing Method to start a small response chain </summary>
		/// <param name="msg"> RPCMessage info </param>
		public void Syn(RPCMessage msg) {
			Log.Verbose($"Syn from {msg.sender.identity}: {msg[0]}");
			msg.sender.Call(SynAck, msg[0]);
		}

		/// <summary> Testing Method in the middle of a small response chain </summary>
		/// <param name="msg"> RPCMessage info </param>
		public void SynAck(RPCMessage msg) {
			Log.Verbose($"SynAck from {msg.sender.identity}: {msg[0]}");
			msg.sender.Call(Ack, msg[0]);
		}

		/// <summary> Testing Method at the end of a small response chain </summary>
		/// <param name="msg"> RPCMessage info </param>
		public void Ack(RPCMessage msg) {
			Log.Verbose($"Ack from {msg.sender.identity}: {msg[0]}");


		}

	}
}
