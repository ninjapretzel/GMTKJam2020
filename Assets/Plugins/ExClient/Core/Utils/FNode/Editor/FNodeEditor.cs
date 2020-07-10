#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using FNODE;
using UnityEditor.IMGUI.Controls;
using System.Linq;

public class FNodeEditor : EditorWindow{

	public string pathToFile = null;
	public FNode<JsonObject> root = null;
	public string serialized = "{}";
	public FNode<JsonObject> active = null;
	public JsonFNodeTreeView treeView = null;
	private Vector2 dataScroll = Vector2.zero;

	[MenuItem("Window/FNode Editor")]
	static void Init() {
		var window = GetWindow<FNodeEditor>();
		window.titleContent = new GUIContent("FNode Editor");
		window.minSize = new Vector2(800, 600);
		window.Show();
	}
	
	void Awake() { }

	private FNode<JsonObject> tempData() {
		var root = FNode<JsonObject>.NewRoot();
		root.AddChild("yeet");
		root.AddChild("yeah");
		root.AddChild("deep").AddChild("nested").AddChild("goodness");

		return root;
	}
	void OnEnable() {
		if (root == null) { root = tempData(); }
		// if (root == null) { root = FNode<JsonObject>.NewRoot(); }
		if (active == null) { active = root; }
		treeView = new JsonFNodeTreeView();

	}
	void OnDestroy() { }
	void OnFocus() { }
	void OnLostFocus() { }

	void OnHierarchyChange() { }
	void OnInspectorUpdate() { }
	void OnProjectChange() { }
	void OnSelectionChange() { }

	void OnGUI() {
		
		GUILayout.BeginArea(new Rect(0, 0, position.width * .25f, position.height)); {
			treeView.Draw(root, "data");
		} GUILayout.EndArea();
		
		if (treeView.selected != active) {
			active = treeView.selected;
			if (active != null) {
				serialized = active.data.ToString();
			}
		}

		Rect rest = new Rect(position.width * .25f, 0, position.width * .75f, position.height);
		GUILayout.BeginArea(rest); {
			dataScroll = GUILayout.BeginScrollView(dataScroll, false, true); {
			if (active != null) {
					if (GUILayout.Button("Unfocus")) { JsonFNodeTreeViewUtil.Unfocus(); }

					// EditorGUILayout.TextField("Clear", "");

					var lastSerialized = serialized;
					serialized = EditorGUILayout.DelayedTextField(serialized, GUILayout.Height(96));
					if (serialized.Length != lastSerialized.Length || serialized != lastSerialized) {
						try {
							JsonObject jobj = Json.Parse<JsonObject>(serialized);
							active.data.Clear();
							active.data.SetRecursively(jobj);
						} catch (Exception e) {
							Debug.LogWarning("Failed to parse copy/pasted JSON." + e + "\n" + e.StackTrace);
							serialized = lastSerialized;
						}
					}

					bool changed = JsonDrawer.DrawSolo(active.data, "Data");
					if (changed) {
						serialized = active.data.ToString();
					}
				

				
			
			} else {
				GUI.Label(rest, "Nothing to draw.");
			
			}
			} GUILayout.EndScrollView();
		} GUILayout.EndArea();
	}


	void Update() {
		
	}
	
}
public class JsonFNodeTreeView : FNodeTreeView<JsonObject> {
	public JsonFNodeTreeView() { }
}
public class JsonFNodeTreeViewUtil {
	private const string FOCUS_BUSTER = "AYYAAYEEETYEAH";
	private static Rect UNFOCUS_RECT = new Rect(-10000, -10000, 1, 1);

	public static void Unfocus() {
		GUI.SetNextControlName(FOCUS_BUSTER);
		GUI.TextField(UNFOCUS_RECT, "");
		GUI.FocusControl(FOCUS_BUSTER);
	}

}

public abstract class FNodeTreeView<T> {
	private static readonly string[] EMPTY = new string[] { };
	private static readonly GUILayoutOption FIXED_WIDTH = GUILayout.ExpandWidth(false);
	private static readonly GUILayoutOption SMALL_BUTTON = GUILayout.Width(24);
	private static Color[] DEFAULT_COLORS = new Color[] {
		Color.white,
		new Color(.85f, .85f, .85f),
	};
	public static Color ModColor(int i, Color[] colors = null) {
		if (colors == null) { colors = DEFAULT_COLORS; }
		i = (i < 0) ? -i : i;
		return colors[i % colors.Length];
	}

	// private string addNode = "";
	private string removeKey = null;
	private string changeKey = null;
	private string currentPath = "/";
	//private FNode<T> changeTarget = null;
	private FNode<T> active = null;

	private Stack<string> history = new Stack<string>();
	private Stack<FNode<T>> visited = new Stack<FNode<T>>();
	
	private HashSet<string> toggledPaths = new HashSet<string>();
	private bool expandedByDefault = false;

	private Vector2 treeScroll = Vector2.zero;

	public FNode<T> selected { get{ return active; } }
	
	public bool Draw(FNode<T> root, string rootLabel = "Tree") {
		if (root == null) {
			GUILayout.BeginVertical("box"); {
				GUILayout.Label(rootLabel);
				GUILayout.Label("Root FNode is NULL");
			} GUILayout.EndVertical();
			return false;
		}

		if (active == null) {
			active = root;
		}
		removeKey = null;
		changeKey = null;
		currentPath = "/";
		//changeTarget = null;
		visited.Clear();
		history.Clear();
		history.Push(currentPath);


		treeScroll = GUILayout.BeginScrollView(treeScroll, false, true); {
			GUILayout.BeginVertical("box"); {
				DrawNode("root", root);
			} GUILayout.EndVertical();
		} GUILayout.EndScrollView();
		
		return (removeKey != null || changeKey != null);
	}
	private void DrawNode(string name, FNode<T> node) {
		bool expanded = IsExpanded(currentPath);
		GUILayout.BeginHorizontal(); {
			// Delete Button
			if (!node.isRoot) {
				// RemoveButton(node.parent, name);
				GUILayout.Button("x", SMALL_BUTTON);
			}
			GUILayout.Label(name, FIXED_WIDTH);
			// Select Button 
			if (GUILayout.Button(active == node ? "()" : "", SMALL_BUTTON)) {
				active = node;
			}
			GUILayout.Label(": {", FIXED_WIDTH);
			
			if (!expanded) {
				if (GUILayout.Button("...")) { Toggle(currentPath); }
				GUILayout.Label("}");
				GUILayout.FlexibleSpace();
			} else {
				if (GUILayout.Button("---", SMALL_BUTTON)) {
					Toggle(currentPath);
				}
			}
		} GUILayout.EndHorizontal();

		if (expanded) {
			GUI.color = ModColor(visited.Count);
			GUILayout.BeginVertical("box"); {
				GUI.color = Color.white;
				
				var childrens = node.childrenNames;
				foreach (var childName in childrens) {
					var child = node.GetChild(childName);
					Push(node, child);
					DrawNode(childName, child);
					Pop();
				}

				FieldControls(node);

			} GUILayout.EndVertical();
		}

	}
	private void FieldControls(FNode<T> node) {

	}

	private void Push(FNode<T> now, FNode<T> next) {
		history.Push(currentPath);
		currentPath = next.path;
		visited.Push(now);
	}
	private void Pop() {

		visited.Pop();
		currentPath = history.Pop();
	}
	
	private bool IsExpanded(string path) {
		if (toggledPaths.Contains(path)) { return !expandedByDefault; }
		return expandedByDefault;
	}
	private void Toggle(string path) {
		if (toggledPaths.Contains(path)) { toggledPaths.Remove(path); }
		else { toggledPaths.Add(path); }
	}
}

#endif
