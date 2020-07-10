using System;
using System.Collections;
using System.Collections.Generic;
using static FNODE.ConnectResult;

namespace FNODE {

	/// <summary> Result for connect/disconnect operations. </summary>
	public enum ConnectResult {
		/// <summary> Connection created successfully. </summary>
		ConnectionCreated = 0,
		/// <summary> Connection removed successfully. </summary>
		ConnectionRemoved = 0,
		/// <summary> Root Nodes have no siblings, and the proposed connection is invalid. </summary>
		SourceIsRootNode = 1,
		/// <summary> The target nodes are not siblings, or not children of the requested node, and the proposed connection is invalid. </summary>
		NotSiblingNodes = 2,
		/// <summary> The key already exists, and must be removed first. </summary>
		ConnectionAlreadyPresent = 3,
		/// <summary> The connection does not exist, and cannot be removed. </summary>
		ConnectionNotPresent = 4,
	}

	/// <summary> 
	/// Generalized, Fractalized, Directed-Linked-Map-N-Tree Node class. 
	/// All FNodes wrap some data object, and may be treated as their wrapped data object. 
	/// Any FNode may have any number of children.
	/// Any FNode contains and maintains information on connections between its children.
	/// Creation of new FNodes is handled by a static Root constructor, or localized Child methods. 
	/// </summary>
	// @TODO: Flesh me out more.
	public sealed class FNode<V> {
		/// <summary> Dictionary alias for Key->Node (transition mappings). </summary>
		public class Transitions : Dictionary<string, FNode<V>> { }
		/// <summary> Dictionary alias for Node->Transitions (neighborhood). </summary>
		public class Nodes : Dictionary<FNode<V>, Dictionary<string, FNode<V>>> { }
		/// <summary> Dictionary alias for string->Node (children). </summary>
		public class NameToNode : Dictionary<string, FNode<V>> { }
		/// <summary> Dictionary alias for Node->string (children). </summary>
		public class NodeToName : Dictionary<FNode<V>, string> { }
		/// <summary> Constant for empty transitions. </summary>
		private static readonly Transitions EMPTY_TRANSITIONS = new Transitions();
		/// <summary> Constant for empty children list. </summary>
		private static readonly List<FNode<V>> EMPTY_CHILDREN = new List<FNode<V>>();
		/// <summary> Constant for empty names list. </summary>
		private static readonly List<string> EMPTY_NAMES = new List<string>();


		public static JsonObject Serialize(FNode<V> node) {
			JsonObject serialized = new JsonObject();
			JsonValue data = Json.Reflect(node.data);

			serialized["data"] = data;

			if (node._children != null) {
				JsonObject children = new JsonObject();
				JsonObject transitions = new JsonObject();
				serialized["children"] = children;
				serialized["transitions"] = transitions;
				foreach (var pair in node._nameToNode) {
					var name = pair.Key;
					var child = pair.Value;
					var links = node._children[child];

					JsonObject serializedChild = Serialize(child);
					children[name] = serializedChild;

					if (links != null && links.Count > 0) {
						JsonObject childTransitions = new JsonObject();
						transitions[name] = childTransitions;
						foreach (var pair2 in links) {
							string from = pair2.Key;
							string to = node._nodeToName[pair2.Value];

							childTransitions[from] = to;
						}

					}
				}

			}

			return serialized;
		}

		public static FNode<V> Deserialize(JsonObject obj) {
			V data = Json.GetValue<V>(obj["data"]);
			FNode<V> node = NewRoot(data);
			DeserializeFill(node, obj);
			return node;
		}

		private static FNode<V> Deserialize(JsonObject obj, FNode<V> parent, string name) {
			V data = Json.GetValue<V>(obj["data"]);
			FNode<V> node = parent.AddChild(name, data);
			DeserializeFill(node, obj);
			return node;
		}

		private static void DeserializeFill(FNode<V> parent, JsonObject obj) {
			if (obj.Has("children")) {
				JsonObject children = obj.Pull<JsonObject>("children");
				JsonObject transitions = obj.Pull<JsonObject>("transitions");

				foreach (var pair in children) {
					string name = pair.Key;
					JsonObject serializedChild = pair.Value as JsonObject;

					FNode<V> child = Deserialize(serializedChild, parent, name);
				}

				foreach (var pair in transitions) {
					string name = pair.Key;
					FNode<V> child = parent.GetChild(name);
					JsonObject links = pair.Value as JsonObject;

					foreach (var pair2 in links) {
						var transitionName = pair2.Key;
						var targetName = pair2.Value.stringVal;
						parent.Connect(transitionName, child, parent.GetChild(targetName));
					}
				}

			}

		}


		/// <summary> Data item inside the node </summary>
		public V data { get; private set; }

		/// <summary> Relative path to this node, from root (root being "/") </summary>
		public string path { get; private set; }

		/// <summary> Parent node to this node. If null, this is a root node. </summary>
		public FNode<V> parent { get; private set; }

		/// <summary> Holds relationship between FNodes within this FNode. Lazily initalized. </summary>
		private Nodes _children;
		/// <summary> Holds name to child. </summary>
		private NameToNode _nameToNode;
		/// <summary> Holds child to name. </summary>
		private NodeToName _nodeToName;

		/// <summary> children of this FNode </summary>
		public IEnumerable<FNode<V>> children {
			get {
				if (_children == null) { return EMPTY_CHILDREN; }
				return _children.Keys;
			}
		}

		/// <summary> Names of children of this FNode </summary>
		public IEnumerable<string> childrenNames {
			get {
				if (_children == null) { return EMPTY_NAMES; }
				return _nameToNode.Keys;
			}
		}

		/// <summary> Internal modifiable transitions </summary>
		private Transitions _links { get { return isRoot ? EMPTY_TRANSITIONS : (Transitions)parent._children[this]; } }
		/// <summary> Get this node's connections to other nodes </summary>
		public IReadOnlyDictionary<string, FNode<V>> links {
			get {
				return isRoot ? EMPTY_TRANSITIONS : parent._children[this];
			}
		}

		/// <summary> Query for root status. Returns (parent == null) </summary>
		public bool isRoot { get { return parent == null; } }

		/// <summary> Implicit conversion from a node, to the data it contains. </summary>
		public static implicit operator V(FNode<V> node) { return node.data; }

		/// <summary> Treat this node as a dictionary, and follow its transition for <paramref name="key"/>. 
		/// Assigning to this will reassign transitions. Assigning null only removes transitions. </summary>
		/// <param name="key"> Key of transition to follow or change </param>
		/// <returns> Transition destination, or null if there is no transition for <paramref name="key"/>. </returns>
		public FNode<V> this[string key] {
			get {
				if (links == null) { return null; }
				if (links.ContainsKey(key)) { return links[key]; }
				return null;
			}
			set {
				Disconnect(key);
				if (value != null) {
					Connect(key, value);
				}
			}
		}

		/// <summary> Convinience method to create root instances with types that have an empty parameter constructor. </summary>
		/// <returns> FNode root object. </returns>
		public static FNode<V> NewRoot() {
			V raw = Activator.CreateInstance<V>();
			var root = new FNode<V>(raw);
			root.path = "/";
			return root;
		}
		/// <summary> Creates a new FNode root. </summary>
		/// <param name="data"> Root data object to wrap. </param>
		/// <returns> FNode root object. </returns>
		public static FNode<V> NewRoot(V data) {
			var root = new FNode<V>(data);
			root.path = "/";
			return root;
		}

		/// <summary> Private constructor for a new FNode. </summary>
		/// <param name="data"> Data object to wrap. </param>
		private FNode(V data) {
			this.data = data;
			this.parent = null;
		}

		/// <summary> Adds a new child object, with a default constructed <typeparamref name="V"/>. </summary>
		/// <param name="name"> Name of child object to add</param>
		/// <returns> Newly created FNode. </returns>
		public FNode<V> AddChild(string name) {
			return AddChild(name, Activator.CreateInstance<V>());
		}

		/// <summary> Creates a child node to this FNode. </summary>
		/// <param name="data"> Data object to wrap. </param>
		/// <returns> Newly created FNode. </returns>
		public FNode<V> AddChild(string name, V data) {
			if (name.Contains("/")) {
				throw new InvalidOperationException("Names may not have '/' in them.");
			}

			var node = new FNode<V>(data);
			node.parent = this;
			node.path = isRoot ? path + name : path + "/" + name;

			if (_children == null) {
				_children = new Nodes();
				_nameToNode = new NameToNode();
				_nodeToName = new NodeToName();
			}

			_children[node] = new Transitions();
			_nameToNode[name] = node;
			_nodeToName[node] = name;
			return node;
		}

		/// <summary> Gets the first child that matches the given <paramref name="predicate"/>. </summary>
		/// <param name="predicate"> Predicate function to match </param>
		/// <returns> First child that matches the predicate. </returns>
		public FNode<V> FindChild(Func<V, bool> predicate) {
			foreach (var node in _children.Keys) {
				if (predicate(node)) { return node; }
			}
			return null;
		}

		/// <summary> Gets the first child who's data matches the given	<paramref name="match"/> data. </summary>
		/// <param name="match"> Data to match by <see cref="object.Equals(object,object)"/></param>
		/// <returns> First node that matches, or null if none do. </returns>
		public FNode<V> FindChild(V match) {
			foreach (var node in _children.Keys) {
				if (Equals(node.data, match)) { return node; }
			}
			return null;
		}

		/// <summary> Get the child with a given name, or null if it does not exist. </summary>
		/// <param name="name"> Name of child to check for </param>
		/// <returns> child, or null </returns>
		public FNode<V> GetChild(string name) {
			if (_nameToNode.ContainsKey(name)) {
				return _nameToNode[name];
			}
			return null;
		}

		/// <summary> Get the name of a potential child. </summary>
		/// <param name="node"> Node to check name of </param>
		/// <returns> Name of node, if it is a child, or null if it is not. </returns>
		public string GetName(FNode<V> node) {
			if (_nodeToName.ContainsKey(node)) {
				return _nodeToName[node];
			}
			return null;
		}

		/// <summary> Gets all children nodes whose data matches the given <paramref name="predicate"/>. </summary>
		/// <param name="predicate"> Predicate function to match </param>
		/// <returns> List of all Nodes that match the predicate, or an empty list if it does not. </returns>
		public List<FNode<V>> GetChildren(Func<V, bool> predicate) {
			List<FNode<V>> nodes = new List<FNode<V>>();
			foreach (var node in _children.Keys) {
				if (predicate(node)) { nodes.Add(node); }
			}
			return nodes;
		}

		/// <summary> Gets all the children nodes whose data matches the given <paramref name="match"/> data. </summary>
		/// <param name="match"> Data to match by <see cref="object.Equals(object, object)"/></param>
		/// <returns> All nodes that match the given data. </returns>
		public List<FNode<V>> GetChildren(V match) {
			List<FNode<V>> nodes = new List<FNode<V>>();
			foreach (var node in _children.Keys) {
				if (Equals(node.data, match)) { nodes.Add(node); }
			}
			return nodes;
		}

		/// <summary> Query if the given <paramref name="node"/> is a direct child of this node. </summary>
		/// <param name="node"> Node to check child status of </param>
		/// <returns> true if <paramref name="node"/> is a child, otherwise false. </returns>
		public bool HasChild(FNode<V> node) {
			return _children != null && _children.ContainsKey(node);
		}

		/// <summary> Connects <paramref name="source"/> to <paramref name="destination"/>, 
		/// with <paramref name="key"/> as description/key. </summary>
		/// <param name="key"> Description of connection </param>
		/// <param name="source"> Source of transition to create </param>
		/// <param name="destination"> Destination of transition to create. </param>
		/// <returns> Result describing connection success or failure reason </returns>
		public ConnectResult Connect(string key, FNode<V> source, FNode<V> destination) {
			if (!HasChild(source) || !HasChild(destination)) { return NotSiblingNodes; }
			if (source[key] != null) { return ConnectionAlreadyPresent; }

			_children[source][key] = destination;

			return ConnectionCreated;
		}

		/// <summary> Creates a connection between this node and <paramref name="destination"/>. </summary>
		/// <param name="destination"> Destination of transition to create. </param>
		/// <returns> Result describing connection success or failure reason </returns>
		public ConnectResult Connect(string key, FNode<V> destination) {
			if (isRoot) { return SourceIsRootNode; }
			return parent.Connect(key, this, destination);
		}

		/// <summary> Disconnects <paramref name="source"/> from <paramref name="destination"/>, if possible. </summary>
		/// <param name="source"> Source of transition to remove. </param>
		/// <param name="destination"> Destination of transition to remove. </param>
		/// <returns> Result describing connection success or failure reason </returns>
		public ConnectResult Disconnect(string key, FNode<V> source) {
			if (!HasChild(source)) { return NotSiblingNodes; }
			if (source[key] == null) { return ConnectionNotPresent; }

			_children[source].Remove(key);
			return ConnectionRemoved;
		}

		/// <summary> Disconnect this node from <paramref name="destination"/>, if possible. </summary>
		/// <param name="destination"> Destination of transition to remove. </param>
		/// <returns> Result describing connection success or failure reason </returns>
		public ConnectResult Disconnect(string key) {
			if (isRoot) { return SourceIsRootNode; }
			return parent.Disconnect(key, this);
		}

		/// <summary> Compare all elements within a FNode structure with another. </summary>
		/// <param name="other"> Other structure to compare to </param>
		/// <returns> True if the structures are entirely equal (data, children, names, transitions), otherwise false. </returns>
		public bool RecursiveEquals(FNode<V> other) {
			if (ReferenceEquals(this, other)) { return true; }

			if (!other.data.Equals(data)) { return false; }
			if ((_children != null) != (other._children != null)) { return false; }

			if (_children != null && other._children != null) {
				if (_children.Count != other._children.Count) { return false; }
				foreach (var pair in _nameToNode) {
					var name = pair.Key;
					var child = pair.Value;

					if (!other._nameToNode.ContainsKey(name)) { return false; }
					if (!other._nameToNode[name].RecursiveEquals(child)) { return false; }

					var transitions = _children[child];
					var otherChild = other._nameToNode[pair.Key];
					var otherTransitions = other._children[otherChild];

					if (transitions.Count != otherTransitions.Count) { return false; }
					foreach (var pair2 in transitions) {
						var transitionName = pair2.Key;
						var transitionTarget = pair2.Value;

						if (!otherTransitions.ContainsKey(transitionName)) { return false; }
						var otherTransitionTarget = otherTransitions[transitionName];
						var targetName = _nodeToName[transitionTarget];
						var otherTargetName = other._nodeToName[otherTransitionTarget];

						if (targetName != otherTargetName) { return false; }
					}

				}
			}

			return true;
		}

	}

}
