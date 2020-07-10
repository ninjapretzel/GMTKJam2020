using System;
using System.Collections;
using System.Collections.Generic;
using BakaTest;
using FNODE;
using FF = FNODE.FNode<string>;
using FJ = FNODE.FNode<JsonObject>;
using System.Linq;
using Ex.Utils;

public static class FNode_Tests {


	public static FF SetupData1() {
		var root = FF.NewRoot("hi");
		var hello = root.AddChild("hello", "hello");
		var nurse = root.AddChild("nurse", "nurse");
		var world = root.AddChild("world", "world");

		hello.Connect("programming", world);
		hello.Connect("animaniacs", nurse);

		return root;
	}

	public static FF SetupData2() {
		var root = FF.NewRoot("dungeon1");
		var entry = root.AddChild("Entry", "Entry");
		var hallway = root.AddChild("Hallway", "Hallway");
		var bathroom = root.AddChild("Bathroom", "Bathroom");
		var pit = root.AddChild("Pit", "Pit");
		var outside = root.AddChild("Outside", "Outside");

		entry.Connect("West", hallway);
		entry.Connect("Leave", outside);

		hallway.Connect("East", entry);
		hallway.Connect("West", bathroom);
		hallway.Connect("Hole", pit);

		bathroom.Connect("East", hallway);

		pit.Connect("Tunnel", outside);

		outside.Connect("Enter", entry);

		return root;
	}

	private class GenData {
		public readonly string thing;
		public readonly int min, max;
		public GenData(string thing, int min, int max) {
			this.thing = thing;
			this.min = min;
			this.max = max;
		}
	}

	private static FJ Fill(SRNG rng, FJ parent, int index, GenData[] data) {
		var datum = data[index];
		var num = rng.NextInt(datum.min, datum.max);

		for (int i = 0; i < num; i++) {
			var node = parent.AddChild(datum.thing + rng.NextInt(10000, 99999));
			if (index != data.Length - 1) {
				Fill(rng, node, index + 1, data);
			}
		}

		return parent;
	}

	// Unfortunately, this ends up relying on the SRNG.
	// oh well, it is the most compact way to test this kind of structure.
	public static FJ SetupData3() {
		var root = FJ.NewRoot(new JsonObject("name", "Universe9001"));
		SRNG rng = new SRNG(331337);
		return Fill(rng, root, 0, new GenData[] {
			new GenData("Galaxy", 1, 4),
			new GenData("System", 2, 10),
			new GenData("Planet", 1, 7),
			new GenData("Moon", 0, 5)
		});
	}

	public static void TestConnect() {
		var abcRoot = FF.NewRoot("");
		var a = abcRoot.AddChild("a", "a");
		var b = abcRoot.AddChild("b", "b");

		// First connection should work.
		a.Connect("to b", b)
			.ShouldBe(ConnectResult.ConnectionCreated);
		// Second time should not, as it already exists.
		a.Connect("to b", b)
			.ShouldBe(ConnectResult.ConnectionAlreadyPresent);
		// Connecting in the other direction should work.
		b.Connect("to a", a)
			.ShouldBe(ConnectResult.ConnectionCreated);
		// Second time should not, as it already exists.
		b.Connect("to a", a)
			.ShouldBe(ConnectResult.ConnectionAlreadyPresent);

		// Connecting to the same node via a different transition
		// should be valid.
		a.Connect("also to b", b)
			.ShouldBe(ConnectResult.ConnectionCreated);

	}

	public static void TestImplicitConversion() {
		var root = SetupData1();

		string rootStr = root;
		rootStr.ShouldBe("hi");

	}

	public static void TestConnections1() {
		var root = SetupData1();

		var hello = root.FindChild("hello");
		var helloConns = hello.links;
		var hello2 = root.FindChild(it => it.Equals("hello"));
		var hello2Conns = hello2.links;

		helloConns.Count.ShouldBe(2);
		hello2Conns.Count.ShouldBe(2);

		var world = root.FindChild(it => it == "world");
		var worldConns = world.links;

		var thing = root.AddChild("thing1", "thing");
		var thing2 = root.AddChild("thing2", "thing");
		root.GetChildren("thing").Count.ShouldBe(2);
		thing.ShouldNotBe(thing2);
	}

	public static void TestConnections2() {
		var root = SetupData2();
		var room = root.FindChild("Entry");

		var pit = room["West"]["Hole"];
		pit.ShouldBe(root.FindChild("Pit"));

		var bathroom = room["West"]["West"];
		bathroom.ShouldBe(root.FindChild("Bathroom"));

		var outside1 = room["West"]["Hole"]["Tunnel"];
		var outside2 = room["Leave"];

		outside1.ShouldBe(outside2);
		outside1.ShouldBe(root.FindChild("Outside"));

		var bathroomInsane =
			room["West"]["Hole"]["Tunnel"]["Enter"]["Leave"]["Enter"]["West"]["West"];

		bathroom.ShouldBe(bathroomInsane);
	}

	public static void TestReconnect() {
		var root = SetupData2();
		var wonderland = root.AddChild("Wonderland", "Wonderland");

		var entry = root.FindChild("Entry");
		entry["Leave"] = wonderland;

		var outside = root.FindChild("Outside");
		var outsideNav = outside["Enter"]["West"]["Hole"]["Tunnel"];
		outsideNav.ShouldBe(outside);

		var notOutside = entry["Leave"];
		var alsoNotOutside = outsideNav["Enter"]["Leave"];

		notOutside.ShouldBe(wonderland);
		alsoNotOutside.ShouldBe(wonderland);


	}


	public static void TestSimpleGeneration() {
		var root = SetupData3();
		StringBuilder str = "";
		Action<FJ> print = (it) => { };
		print = (it) => {
			str = str + it.path + " ( " + it.children.Count() + " children)\n";
			foreach (var child in it.children) { print(child); }
		};
		print(root); 
		// Debug.Log(str);

	}

	public static void TestPaths() {
		var root = FJ.NewRoot();
		var ayy = root.AddChild("ayy");
		var bee = ayy.AddChild("bee");
		var cee = bee.AddChild("cee");

		cee.path.ShouldBe("/ayy/bee/cee");

	}

	public static void TestInitializations() {
		var root = FJ.NewRoot();
		JsonObject obj = root;
		obj.ShouldNotBe(null);
	}

	public static void TestRecurisveEquals() {
		{
			var root1a = SetupData1();
			var root1b = SetupData1();
			root1a.RecursiveEquals(root1a).ShouldBeTrue();
			root1b.RecursiveEquals(root1b).ShouldBeTrue();
			root1a.RecursiveEquals(root1b).ShouldBeTrue();
			root1b.RecursiveEquals(root1a).ShouldBeTrue();
		}

		{
			var root1 = SetupData1();
			var root2 = SetupData2();
			root1.RecursiveEquals(root2).ShouldBeFalse();
			root2.RecursiveEquals(root1).ShouldBeFalse();
		}

	}

	public static void TestSerialization() {
		// Only works if values are printed in the order we expect, so we force Dictionary<> to be used.
		var prev = JsonObject.DictionaryGenerator;
		JsonObject.DictionaryGenerator = ()=>{return new Dictionary<JsonString, JsonValue>(); };
		try {

			{
				var root = SetupData1();
				JsonObject json = FF.Serialize(root);
				// Debug.Log(json.PrettyPrint());
				string expected = @"{
	""data"":""hi"",
	""children"":
	{
		""hello"":
		{
			""data"":""hello""
		},
		""nurse"":
		{
			""data"":""nurse""
		},
		""world"":
		{
			""data"":""world""
		}
	},
	""transitions"":
	{
		""hello"":
		{
			""programming"":""world"",
			""animaniacs"":""nurse""
		}
	}
}";
				json.PrettyPrint().ShouldBe(expected);
				FF reconstructed = FF.Deserialize(json);

				reconstructed.RecursiveEquals(root).ShouldBe(true);

			}
		} finally {
			JsonObject.DictionaryGenerator = prev;
		}
	}

}
