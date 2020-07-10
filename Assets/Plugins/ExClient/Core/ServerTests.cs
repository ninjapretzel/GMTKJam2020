
#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
using UnityEngine;
#else

// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public static class Server_Tests {

	private class TestData {
		public Server server { get; private set; }
		public Client admin { get; private set; }
		public TestData(Server server, Client admin) {
			this.server = server;
			this.admin = admin;
		}

	}

	private static TestData DefaultSetup(string testDbName = "Testing", string testDb = "../../../db", int port = 12345, float tick = 50) {
		Server server = new Server(port, tick);
		server.AddService<DebugService>();
		server.AddService<LoginService>();
		server.AddService<EntityService>();
		server.AddService<MapService>();

		var serverSync = server.AddService<SyncService>();
		{
			var debugSync = serverSync.Context("debug");
			JsonObject data = new JsonObject();
			data["gameState"] = new JsonObject("gravity", 9.8f, "tickrate", 100);
			data["Test"] = new JsonObject("blah", "blarg", "Only", "Top", "Level", "Objects", "Get", "Syncd");
			data["Of"] = "Not an object, This doesn't get sync'd";
			data["Data"] = new JsonArray("Not", "an", "object,", "Neither", "does", "this");
			debugSync.SetData(data);
			debugSync.DefaultSubs("Test", "Data");

		}

		server.AddService<DBService>()
			.Connect()
			.UseDatabase(testDbName)
			.CleanDatabase()
			.Reseed(testDb)
			;
		server.Start();
		Thread.Sleep(50);

		Client admin = new Client(new TcpClient("localhost", port));
		admin.AddService<DebugService>();
		admin.AddService<LoginService>();
		admin.AddService<EntityService>();
		admin.AddService<MapService>();
		var adminSync = admin.AddService<SyncService>();
		
		admin.ConnectSlave();
		admin.Call(Members<LoginService>.i.Login, "admin", "admin", VersionInfo.VERSION);
		adminSync.Context("debug").SubscribeTo("gameState");
		Thread.Sleep(50);

		
		return new TestData(server, admin);
	}

	private static void CleanUp(TestData data) {
		Log.Info("Waiting...");
		Thread.Sleep(500);
		Log.Info("Cleaning Up NOW.");
		data.admin.server.Stop();
		data.server.Stop();
	}

	public static void Test_Integrated_Generator() {

		var testData = DefaultSetup();
		// defer CleanUp(testData);
		try {


			var db = testData.server.GetService<DBService>();
			var data = db.GetData("Content", "ItemGeneration", "filename", "Materials");
		
			var gen = new Generator(data);
		
			ItemGenSeed igSeed_1_0 = new ItemGenSeed(Guid.Parse("3f162ba3-d167-4888-a26b-c193615e2af1"));
			ItemGenSeed igSeed_2_0 = new ItemGenSeed(Guid.Parse("819f5900-9794-4d64-9aa9-e45e0dae9cde"));
			ItemGenSeed igSeed_2_1 = igSeed_2_0.Next();

			var result = gen.Generate("Mineral", igSeed_1_0);

			Console.WriteLine(result.PrettyPrint());

		} finally {

			CleanUp(testData);
		}
		

	}
	
}
#endif
#endif
