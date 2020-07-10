using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ex {

	public class RPCMessage {
		/// <summary>Separator character to separate segments of transmissions</summary>
		public const char SEPARATOR = (char)0x07; // 'Bell'
		/// <summary> End Of Transmission </summary>
		public const char EOT = (char)0x1F; // 'Unit Separator'
		/// <summary> Fixed Size of the message's reserved spaces </summary>
		public const int FIXED_SIZE = 3;
		/// <summary> Reserved position for Service name </summary>
		public const int SERVICE_NAME = 0;
		/// <summary> Reserved position for Method name </summary>
		public const int METHOD_NAME = 1;
		/// <summary> Reserved position for Sender's UTC timestamp </summary>
		public const int SENDER_TIME = 2;
		
		/// <summary> Client message was recieved from </summary>
		public Client sender;

		/// <summary> Raw message sent </summary>
		public string rawMessage { get; private set; }
		/// <summary> UTC Timestamp when instance was created </summary>
		public DateTime recievedAt { get; private set; }
		/// <summary> UTC Timestamp when instance was sent </summary>
		public DateTime sentAt { get; private set; }

		/// <summary> Service name to look up service </summary>
		public string serviceName { get { return content[SERVICE_NAME]; } }
		/// <summary> Method name to look up method </summary>
		public string methodName { get { return content[METHOD_NAME]; } }
		/// <summary> Name of RPC to call (serviceName.methodName)</summary>
		public string rpcName { get { return $"{content[SERVICE_NAME]}.{content[METHOD_NAME]}"; } }


		/// <summary> Raw content of message </summary>
		public string[] content { get; private set; }
		/// <summary> Number of arguments, besides service name/method name </summary>
		public int numArgs { get { return content.Length - FIXED_SIZE; } }
		/// <summary> Returns true if this was sent over UDP, false if was sent over TCP. </summary>
		public bool wasUDP { get; private set; } = false;

		/// <summary> Creates an RPCMessage flagged as if it came over UDP. </summary>
		/// <param name="client">client who sent</param>
		/// <param name="str">string read from the stream</param>
		/// <returns></returns>
		public static RPCMessage UDP(Client client, string str) {
			RPCMessage msg = new RPCMessage(client, str);
			msg.wasUDP = true;
			return msg;
		}
		/// <summary> Creates an RPCMessage flagged as if it came over TCP. </summary>
		/// <param name="client">client who sent</param>
		/// <param name="str">string read from the stream</param>
		/// <returns></returns>
		public static RPCMessage TCP(Client client, string str) {
			RPCMessage msg = new RPCMessage(client, str);
			msg.wasUDP = false;
			return msg;
		}


		/// <summary> Indexer for arguments. Valid indexes are in [0, numArgs-1] </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string this[int index] { get { return content[index + FIXED_SIZE]; } }

		
		/// <summary> Constructs an RPC message sent from the given client </summary>
		/// <param name="client"></param>
		/// <param name="str"></param>
		private RPCMessage(Client client, string str) {
			sender = client;
			rawMessage = str;
			recievedAt = DateTime.UtcNow;
			content = rawMessage.Split(SEPARATOR);
			sentAt = Utils.Unpack.Base64<DateTime>(content[SENDER_TIME]);
		}

		public void Reply(RPCMessage.Handler callback, params System.Object[] stuff) {
			if (wasUDP) {
				sender.Hurl(callback, stuff);
			} else {
				sender.Call(callback, stuff);
			}
		}


		public void Forward(IEnumerable<Client> clients, RPCMessage.Handler callback, params System.Object[] stuff) {

		}


		/// <summary> Delegate type used to search for messages to invoke from network messages </summary>
		/// <param name="Client"> Client whomst'd've sent the message </param>
		/// <param name="message"> Message that was sent </param>
		public delegate void Handler(RPCMessage message);
	}

}
