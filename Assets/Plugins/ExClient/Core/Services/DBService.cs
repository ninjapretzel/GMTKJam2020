#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
using UnityEngine;
#else

#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using MongoDB.Driver;
using MongoDB.Bson;
using BDoc = MongoDB.Bson.BsonDocument;
using MDB = MongoDB.Driver.IMongoDatabase;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization;
using Ex.Utils;
using System.IO;
using Ex.Data;
using MongoDB.Bson.Serialization.Serializers;
using System.Runtime.CompilerServices;
using MongoDB.Bson.IO;
#endif

namespace Ex {

#if !UNITY
	/// <summary> Base class for any types being stored in the database. 
	/// Standardizes access to the MongoDB "_id" property, a single relational Guid,</summary>
	public class DBEntry {
		/// <summary> MongoDB "_id" property for uniqueness </summary>
		[BsonId] public ObjectId id { get; set; }
		/// <summary> Guid to relate entry to some specific entity or user. </summary>
		public Guid guid { get; set; }
	}

	/// <summary> Provides the same as the <see cref="DBEntry"/> class, 
	/// and also a generic 'data' object for arbitrary data. </summary>
	public class DBData : DBEntry {
		/// <summary> Used to defer storage of arbitrary data. </summary>
		public JsonObject data;
		/// <summary> Helpful macro that grabs the calling member name of anything that calls it. 
		/// <para>Makes it easier to make properties utilizing the <see cref="data"/> field, eg </para> <para><code>
		/// public <see cref="JsonObject"/> Attributes { get { return data.Get&lt;<see cref="JsonObject"/>&gt;(MemberName()); } }
		/// </code></para></summary>
		/// <param name="caller"> Autofilled by compiler </param>
		/// <returns> Name of member calling this method. </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string MemberName([CallerMemberName] string caller = null) { return caller; }
		public DBData() : base() { data = new JsonObject(); }
	}
	
#endif 

	/// <summary> Service type for holding database connection. Empty on client. </summary>
	public class DBService : Service {
#if !UNITY
		// Todo: find these and register them automatically
		public static void RegisterSerializers() {
			BsonSerializer.RegisterSerializer<Rect>(new RectSerializer());
			BsonSerializer.RegisterSerializer<Bounds>(new BoundsSerializer());
			BsonSerializer.RegisterSerializer<Ray>(new RaySerializer());
			BsonSerializer.RegisterSerializer<Ray2D>(new Ray2DSerializer());
			BsonSerializer.RegisterSerializer<Plane>(new PlaneSerializer());
			BsonSerializer.RegisterSerializer<RectInt>(new RectIntSerializer());
			BsonSerializer.RegisterSerializer<Vector2>(new Vector2Serializer());
			BsonSerializer.RegisterSerializer<Vector2Int>(new Vector2IntSerializer());
			BsonSerializer.RegisterSerializer<Vector3>(new Vector3Serializer());
			BsonSerializer.RegisterSerializer<Vector3Int>(new Vector3IntSerializer());
			BsonSerializer.RegisterSerializer<Vector4>(new Vector4Serializer());
			
			BsonSerializer.RegisterSerializer<InteropFloat64>(new InteropFloat64Serializer());
			BsonSerializer.RegisterSerializer<InteropFloat32>(new InteropFloat32Serializer());
			BsonSerializer.RegisterSerializer<InteropString32>(new InteropString32Serializer());
			BsonSerializer.RegisterSerializer<InteropString256>(new InteropString256Serializer());

			BsonSerializer.RegisterSerializer<UberData>(new UberDataSerializer());
			BsonSerializer.RegisterSerializer<SimplexNoise>(new SimplexNoiseSerializer());

			BsonSerializer.RegisterSerializer<JsonObject>(new JsonObjectSerializer());
			BsonSerializer.RegisterSerializer<JsonArray>(new JsonArraySerializer());

		}
		
		/// <summary> MongoDB Connection </summary>
		public MongoClient dbClient { get; private set; }
		/// <summary> Database to use by default </summary>
		public MDB defaultDB { get; private set; }
		public string dbName { get; private set; } = "debug";
		public bool cleanedDB { get; private set; } = false;
		
		public override void OnEnable() {
		}

		private static string Filename(string filepath) {
			return FromLast("/", ForwardSlashPath(filepath));
		}
		private static string Folder(string filepath) {
			return UpToLast(filepath, "/");
		}

		private static string UpToLast(string str, string search) {
			if (str.Contains(search)) {
				int ind = str.LastIndexOf(search);
				return str.Substring(0, ind);
			}
			return str;
		}
		private static string ForwardSlashPath(string path) { return path.Replace('\\', '/'); }
		private static string FromLast(string str, string search) {
			if (str.Contains(search) && !str.EndsWith(search)) {
				int ind = str.LastIndexOf(search);

				return str.Substring(ind + 1);
			}
			return "";
		}


		/// <summary> Reseeds the DB service, given it is connected, with instructions in a given directory. </summary>
		/// <param name="dir"> Directory to reseed from </param>
		public void Reseed(string dir) {
			dir = ForwardSlashPath(dir);
			if (!dir.EndsWith("/")) { dir += "/"; }
			try {
				string json = File.ReadAllText(dir + "seed.json");
				JsonValue v = Json.Parse(json);
				
				if (v is JsonArray) {
					Reseed(v as JsonArray, dir);
				} else if (v is JsonObject) {
					Reseed(v as JsonObject, dir);
				}
				
			} catch (Exception e) {
				Log.Error($"Error while seeding database from [{dir}]", e);
			}
		}

		/// <summary> Reseed using all of the given descriptors. </summary>
		/// <param name="descriptors"> JsonArray of JsonObjects describing how to reseed the database. </param>
		/// <param name="dir"> Base directory to reseed from. </param>
		public void Reseed(JsonArray descriptors, string dir = null) {
			foreach (var it in descriptors) {
				if (it is JsonObject) { Reseed(it as JsonObject, dir); }
			}
		}

		/// <summary> Reseeds a database using the given descriptor. </summary>
		/// <param name="reseedInfo"> JsonObject containing description of how to reseed the database. </param>
		public void Reseed(JsonObject reseedInfo, string topDir = null) {
			if (reseedInfo.Has<JsonArray>("drop")) { Drop(reseedInfo.Get<JsonArray>("drop")); }
			if (reseedInfo.Has<JsonObject>("index")) { Index(reseedInfo.Get<JsonObject>("index")); }
			if (reseedInfo.Has<JsonObject>("insert")) { Insert(reseedInfo.Get<JsonObject>("insert"), topDir); }
			
			void Drop(JsonArray databases) {
				foreach (var dbname in databases) {
					if (dbname.isString) {
						dbClient.DropDatabase(dbname.stringVal);
					}
				}
			}

			void Index(JsonObject descriptor) {
				string database = descriptor.Pull("database", dbName);
				string collection = descriptor.Pull("collection", "Garbage");
				if (database == "$default") { database = dbName; }

				JsonObject fields = descriptor.Pull<JsonObject>("fields");
				MDB db = dbClient.GetDatabase(database);

				List<CreateIndexModel<BDoc>> indexes = new List<CreateIndexModel<BDoc>>();
				IndexKeysDefinition<BDoc> index = null;
				foreach (var pair in fields) {
					string fieldName = pair.Key;
					int order = pair.Value;

					if (order > 0) {
						index = index?.Ascending(fieldName) ?? Builders<BDoc>.IndexKeys.Ascending(fieldName);
					} else {
						index = index?.Descending(fieldName) ?? Builders<BDoc>.IndexKeys.Descending(fieldName);
					}
				}

				var model = new CreateIndexModel<BDoc>(index);
				db.GetCollection<BDoc>(collection).Indexes.CreateOne(model);
			}

			void Insert(JsonObject descriptor, string dir) {
				string database = descriptor.Pull("database", dbName);
				string collection = descriptor.Pull("collection", "Garbage");

				string[] files = descriptor.Pull<string[]>("files");
				if (files == null) { files = new string[] { collection }; }

				dir = ForwardSlashPath(dir);
				if (!dir.EndsWith("/")) { dir += "/"; }

				foreach (var file in files) {
					string json = null;
					string fpath = dir + file;

					if (file.EndsWith("/**")) {
						string directory = fpath.Replace("/**", "");

						Glob(database, collection, directory);

					} else {
						try { json = json ?? File.ReadAllText(fpath); } catch (Exception) { }
						try { json = json ?? File.ReadAllText(fpath + ".json"); } catch (Exception) { }
						try { json = json ?? File.ReadAllText(fpath + ".wtf"); } catch (Exception) { }

						if (json == null) {
							Log.Warning($"Seeder could not find file {{{ForwardSlashPath(file)}}} under {{{dir}}}");
							continue;
						}

						JsonValue data = Json.Parse(json);
						if (data == null || !(data is JsonObject) && !(data is JsonArray)) {
							Log.Warning($"Seeder cannot use {{{ForwardSlashPath(file)}}} under {{{dir}}}, it is not an object or array.");
							continue;
						}

						if (data is JsonObject) {
							data["filename"] = UpToLast(FromLast(ForwardSlashPath(fpath), "/"), ".");
							InsertData(database, collection, data as JsonObject);
						} else if (data is JsonArray) {
							InsertData(database, collection, data as JsonArray);
						}
					}
				}


			}
			
			void Glob(string database, string collection, string directory) {
				List<string> files = AllFilesInDirectory(directory);
				foreach (string file in files) {
					string json = null;
					try {
						json = json ?? File.ReadAllText(file);
					} catch (Exception e) {
						Log.Warning($"Seeder could not find {{{file}}}.", e);
					}

					try {
						JsonValue data = Json.Parse(json);

						if (data == null || !(data is JsonObject) && !(data is JsonArray)) {
							Log.Warning($"Seeder cannot use {{{ForwardSlashPath(file)}}}, it is not an object or array.");
							continue;
						}

						if (data is JsonObject) {
							data["filename"] = UpToLast(FromLast(ForwardSlashPath(file), "/"), ".");
							InsertData(database, collection, data as JsonObject);
						} else if (data is JsonArray) {
							InsertData(database, collection, data as JsonArray);
						}

					} catch (Exception e) {
						Log.Warning($"Seeder could not parse {{{ForwardSlashPath(file)}}}.", e);
					}
				}
			}

		}

		private List<string> AllFilesInDirectory(string directory, List<string> collector = null) {
			collector = collector ?? new List<string>();
			var files = Directory.GetFiles(directory);
			collector.AddRange(files);
			//collector.AddRange(files.Select(it => ForwardSlashPath(it)));
			
			var dirs = Directory.GetDirectories(directory);
			foreach (var dir in dirs) {
				AllFilesInDirectory(dir, collector);
			}

			return collector;
		}

		/// <summary> Creates a <see cref="BDoc"/> out of every <see cref="JsonObject"/> in <paramref name="data"/>, and inserts each as a new record in the given <paramref name="database"/> and <paramref name="collection"/>. </summary>
		/// <param name="database"> Database to add data to </param>
		/// <param name="collection"> Collection to add data to </param>
		/// <param name="data"> Data to insert insert </param>
		public void InsertData(string database, string collection, JsonArray vals) {
			List<BDoc> docs = new List<BDoc>(vals.Count);

			foreach (var data in vals) {
				if (data is JsonObject) {
					docs.Add(ToBson(data as JsonObject));
					// InsertData(database, collection, data as JsonObject);
				}
			}

			if (docs.Count > 0) {
				MDB db = dbClient.GetDatabase(database);
				db.GetCollection<BDoc>(collection).InsertMany(docs);
			}

		}

		/// <summary> Creates a <see cref="BDoc"/> out of <paramref name="data"/>, and inserts a new record in the given <paramref name="database"/> and <paramref name="collection"/>. </summary>
		/// <param name="database"> Database to add data to </param>
		/// <param name="collection"> Collection to add data to </param>
		/// <param name="data"> Data to turn into a BDoc and insert </param>
		public void InsertData(string database, string collection, JsonObject data) {
			BDoc doc = ToBson(data);
			
			MDB db = dbClient.GetDatabase(database);
			db.GetCollection<BDoc>(collection).InsertOne(doc);
		}

		/// <summary> Gets raw data from the DB as a JsonObject </summary>
		/// <param name="database"> Database to get out of </param>
		/// <param name="collection"> Collection to get out of </param>
		/// <param name="id"> GUID to get </param>
		/// <returns> Data matching query </returns>
		public JsonObject GetData(string database, string collection, Guid id) {
			var filter = Builders<BsonDocument>.Filter.Eq(nameof(DBEntry.guid), id);
			BDoc result = dbClient.GetDatabase(database).GetCollection<BDoc>(collection).Find(filter).FirstOrDefault();

			return FromBson(result);
		}
		/// <summary> Gets raw data from the DB as a JsonObject </summary>
		/// <param name="database"> Database to get out of </param>
		/// <param name="collection"> Collection to get out of </param>
		/// <param name="idField"> ID field to match </param>
		/// <param name="id"> ID to get from field </param>
		/// <returns> Data matching query </returns>
		public JsonObject GetData(string database, string collection, string idField, string id) {
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			BDoc result = dbClient.GetDatabase(database).GetCollection<BDoc>(collection).Find(filter).FirstOrDefault();

			return FromBson(result);
		}


		/// <summary> Converts a <see cref="BDoc"/> into a <see cref="JsonObject"/></summary>
		/// <param name="data"> Data object to convert </param>
		/// <returns> Converted data</returns>
		private JsonObject FromBson(BDoc data) {
			if (data == null) { return null; }
			JsonObject obj = new JsonObject();
			foreach (var pair in data) {
				string key = pair.Name;
				BsonValue value = pair.Value;

				if (value.IsNumeric) { obj[key] = value.AsDouble; }
				else if (value.IsString) { obj[key] = value.AsString; }
				else if (value.IsBoolean) { obj[key] = value.AsBoolean; }
				else if (value.IsBsonDocument) { obj[key] = FromBson(value.AsBsonDocument); }
				else if (value.IsBsonArray) { obj[key] = FromBson(value.AsBsonArray); }
				else if (value.IsBsonNull) { obj[key] = JsonNull.instance; }
			}
				

			return obj;
		}

		/// <summary> Converts a <see cref="BsonArray"/> into a <see cref="JsonArray"/></summary>
		/// <param name="doc"> Data object to convert </param>
		/// <returns> Converted data</returns>
		private JsonArray FromBson(BsonArray data) {
			if (data == null) { return null; }
			JsonArray arr = new JsonArray();

			foreach (var value in data) {
				if (value.IsNumeric) { arr.Add(value.AsDouble); }
				else if (value.IsString) { arr.Add(value.AsString); }
				else if (value.IsBoolean) { arr.Add(value.AsBoolean); }
				else if (value.IsBsonDocument) { arr.Add(FromBson(value.AsBsonDocument)); }
				else if (value.IsBsonArray) { arr.Add(FromBson(value.AsBsonArray)); }
				else if (value.IsBsonNull) { arr.Add(JsonNull.instance); }
			}

			return arr;
		}

		/// <summary> Converts a <see cref="JsonObject"/> into a <see cref="BDoc"/></summary>
		/// <param name="data"> Data object to convert </param>
		/// <returns> Converted data </returns>
		private BDoc ToBson(JsonObject data) {
			BDoc doc = new BDoc();

			foreach (var pair in data) {
				string key = pair.Key.stringVal;
				JsonValue value = pair.Value;

				if (value.isNumber) { doc[key] = value.doubleVal; }
				else if (value.isString) { doc[key] = value.stringVal; }
				else if (value.isBool) { doc[key] = value.boolVal; }
				else if (value is JsonObject) { doc[key] = ToBson(value as JsonObject); } 
				else if (value is JsonArray) { doc[key] = ToBson(value as JsonArray); }
				else if (value.isNull) { doc[key] = BsonNull.Value; }

			}

			return doc;
		}

		/// <summary> Converts a <see cref="JsonArray"/> into a <see cref="BsonArray"/></summary>
		/// <param name="data"> Data object to convert </param>
		/// <returns> Converted data </returns>
		private BsonArray ToBson(JsonArray data) {
			BsonArray arr = new BsonArray(data.Count);

			foreach (var value in data) {
				
				if (value.isNumber) { arr.Add(value.doubleVal); }
				else if (value.isString) { arr.Add(value.stringVal); }
				else if (value.isBool) { arr.Add(value.boolVal); }
				else if (value is JsonObject) { arr.Add(ToBson(value as JsonObject)); } 
				else if (value is JsonArray) { arr.Add(ToBson(value as JsonArray)); }
				else if (value.isNull) { arr.Add(BsonNull.Value); }

			}
			
			return arr;
		}



		/// <summary> Connects the database to a given mongodb server. </summary>
		/// <param name="location"> Location to connect to, defaults to default mongodb port on localhost </param>
		public DBService Connect(string location = "mongodb://localhost:27017") {
			dbClient = new MongoClient(location);
			defaultDB = dbClient.GetDatabase("debug");
			return this;
		}

		/// <summary> Used to set the default database. </summary>
		public DBService UseDatabase(string dbName) {
			this.dbName = dbName;
			defaultDB = dbClient.GetDatabase(dbName);
			return this;
		}
		
		/// <summary> Cleans (drops) the current database </summary>
		public DBService CleanDatabase() {
			Log.Warning($"Be advised. Clearing Database {{{dbName}}}.");
			dbClient.DropDatabase(dbName);
			defaultDB = dbClient.GetDatabase(dbName);
			cleanedDB = true;
			return this;
		}

		/// <summary> Get a collection of stuff in the default database  </summary>
		/// <typeparam name="T"> Generic type of collection </typeparam>
		/// <returns> Collection of items, using the name of the type </returns>
		public IMongoCollection<T> Collection<T>() where T : DBEntry {
			return defaultDB.GetCollection<T>(typeof(T).Name);
		}
		/// <summary> Get a collection of stuff of a given type in a given database </summary>
		/// <typeparam name="T"> Generic type of collection </typeparam>
		/// <param name="databaseName"> Name of database to sample </param>
		/// <returns> Collection of items, using the name of the type </returns>
		public IMongoCollection<T> Collection<T>(string databaseName) where T : DBEntry {
			return dbClient.GetDatabase(databaseName).GetCollection<T>(typeof(T).Name);
		}

		

		/// <summary> Get a database entry by a general relational guid </summary>
		/// <typeparam name="T"> Generic type of DBEntry Table to get from </typeparam>
		/// <param name="id"> ID to look for 'guid' </param>
		/// <returns> Retrieved result matching the ID, or null </returns>
		public T Get<T>(Guid id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.guid), id);
			T result = Collection<T>().Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get a database entry by a general relational guid </summary>
		/// <typeparam name="T"> Generic type of DBEntry Table to get from </typeparam>
		/// <param name="idField"> ID Field to look for 'guid' within </param>
		/// <param name="id"> ID to look for 'guid' </param>
		/// <returns> Retrieved result matching the ID, or null </returns>
		public T Get<T>(string idField, Guid id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(idField, id);
			T result = Collection<T>().Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get an item from the default database, where ID field matches the given ID, or null. </summary>
		/// <typeparam name="T"> Generic type of item to get </typeparam>
		/// <param name="idField"> Field of ID to match </param>
		/// <param name="id"> ID to match in field </param>
		/// <returns> First item matching id, or null. </returns>
		public T Get<T>(string idField, string id) where T : DBEntry {
			// Todo: Benchmark and figure out which of these is faster
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			T result = Collection<T>().Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get an item from the default database, where ID field matches the given ID, or null. </summary>
		/// <typeparam name="T"> Generic type of item to get </typeparam>
		/// <param name="databaseName"> Name of database to sample </param>
		/// <param name="idField"> Field of ID to match </param>
		/// <param name="id"> ID to match in field </param>
		/// <returns> First item matching id, or null. </returns>
		public T Get<T>(string databaseName, string idField, string id) where T : DBEntry {
			// Todo: Benchmark and figure out which of these is faster
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			T result = Collection<T>(databaseName).Find(filter).FirstOrDefault();
			return result;
		}

		/// <summary> Get all items from the default database, where the given ID field matches the given ID. </summary>
		/// <typeparam name="T"> Generic type of items to get </typeparam>
		/// <param name="idField"> ID Field to look for ID within </param>
		/// <param name="id"> ID to look for ID </param>
		/// <returns> All elements matching the given ID </returns>
		/// <remarks> For example, if `Item` has a field `owner:string`, this can be used to find all `Item`s owned by a given entity. </remarks>
		public List<T> GetAll<T>(string idField, Guid id) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(idField, id);
			List<T> result = Collection<T>().Find(filter).ToList();
			return result;
		}

		/// <summary> Get all items from the default database, where the given ID field matches the given ID. </summary>
		/// <typeparam name="T"> Generic type of items to get </typeparam>
		/// <param name="idField"> ID Field to look for 'guid' within </param>
		/// <param name="id"> ID to look for 'guid' </param>
		/// <returns> All elements matching the given ID </returns>
		/// <remarks> For example, if `Item` has a field `owner:Guid`, this can be used to find all `Item`s owned by a given entity. </remarks>

		public List<T> GetAll<T>(string idField, string id) where T : DBEntry {
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			//var filter = Builders<T>.Filter.Eq(idField, id);

			List<T> result = Collection<T>().Find(filter).ToList();
			return result;
		}

		/// <summary> Get an Enumerable from the given database, where ID field matches the given ID, or null. </summary>
		/// <typeparam name="T"> Generic type of items to get </typeparam>
		/// <param name="databaseName"> Name of database to sample  </param>
		/// <param name="idField"> Field of ID to match </param>
		/// <param name="id"> ID to match in field </param>
		/// <returns> All items with matching id, or an empty list </returns>
		public List<T> GetAll<T>(string databaseName, string idField, string id) where T: DBEntry {
			var filter = BsonHelpers.Query($"{{ \"{idField}\": \"{id}\" }}");
			List<T> result = Collection<T>(databaseName).Find(filter).ToList();
			return result;
		}
		
		/// <summary> Saves the given item into the default database. Updates the item, or inserts it if one does not exist yet.  </summary>
		/// <typeparam name="T"> Generic type of item to insert  </typeparam>
		/// <param name="item"> Item to insert </param>
		public void Save<T>(T item) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.id), item.id);

			var coll = Collection<T>();
			var check = coll.Find(filter).FirstOrDefault();
			
			try {
				if (check == null) {
					coll.InsertOne(item);
				} else {
					var result = coll.ReplaceOne(filter, item);
				}
				
			} catch (Exception e) {
				Log.Error("Failed to save database entry", e);
			}
		}

		/// <summary> Saves the given item into the given database. Updates the item, or inserts it if one does not exist yet.  </summary>
		/// <typeparam name="T"> Generic type of item to insert  </typeparam>
		/// <param name="databaseName"> Name of database to sample </param>
		/// <param name="item"> Item to insert </param>
		public void Save<T>(string databaseName, T item) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.id), item.id);

			var coll = Collection<T>(databaseName);
			var check = coll.Find(filter).FirstOrDefault();

			try {
				if (check == null) {
					coll.InsertOne(item);
				} else {
					var result = coll.ReplaceOne(filter, item);
				}
			} catch (Exception e) {
				Log.Error("Failed to save database entry", e);
			}
		}

		/// <summary> Removes the given item from the default database. </summary>
		/// <typeparam name="T"> Generic type of item to delete </typeparam>
		/// <param name="item"> Item to delete </param>
		public void Remove<T>(T item) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.id), item.id);
			var coll = Collection<T>();
			try {
				coll.DeleteOne(filter);
			} catch (Exception e) {
				Log.Error($"Failed to delete database entry for {typeof(T)}::{item.id}", e);
			}
		}
		/// <summary> Removes all of the data for the given guid from the default database. </summary>
		/// <typeparam name="T"> Generic type of item to delete </typeparam>
		/// <param name="guid"> Guid of data to delete </param>
		public void Remove<T>(Guid guid) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.guid), guid);
			var coll = Collection<T>();
			try {
				coll.DeleteMany(filter);
			} catch (Exception e) {
				Log.Error($"Failed to delete database entries for {typeof(T)}::{guid}", e);
			}
		}

		/// <summary> Removes the given item from the given database. </summary>
		/// <typeparam name="T"> Generic type of item to delete </typeparam>
		/// <param name="databaseName"> Database name to delete </param>
		/// <param name="item"> Item to delete </param>
		public void Remove<T>(string databaseName, T item) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.id), item.id);
			var coll = Collection<T>(databaseName);
			try {
				coll.DeleteOne(filter);
			} catch (Exception e) {
				Log.Error($"Failed to delete database entry for {typeof(T)}::{item.id}", e);
			}
		}

		/// <summary> Removes all of the data for the given guid from the given database. </summary>
		/// <typeparam name="T"> Generic type of item to delete </typeparam>
		/// <param name="databaseName"> Database name to delete from </param>
		/// <param name="guid"> Guid of data to delete </param>
		public void Remove<T>(string databaseName, Guid guid) where T : DBEntry {
			var filter = Builders<T>.Filter.Eq(nameof(DBEntry.guid), guid);
			var coll = Collection<T>(databaseName);
			try {
				coll.DeleteMany(filter);
			} catch (Exception e) {
				Log.Error($"Failed to delete database entries for {typeof(T)}::{guid}", e);
			}
		}
#endif

#if !UNITY
		/// <summary> Helpers for dealing with MongoDB's weirdnesses. </summary>
		public static class BsonHelpers {
			/// <summary> Used to evaluate a query to a BsonDocument object which can be used in most places in MongoDB's API </summary>
			/// <param name="query"> Object literal query </param>
			/// <returns> BsonDocument representing query </returns>
			public static BDoc Query(string query) {
				return MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BDoc>(query);
			}
		}
		
		/// <summary> Icky class to read/write JsonObject fields from/to BSON streams </summary>
		public class JsonObjectSerializer : SerializerBase<JsonObject> {
			public override JsonObject Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
				return context.ReadJsonObject();
			}
			public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonObject value) {
				context.WriteJsonObject(value);
			}
		}

		/// <summary> Icky class to read/write JsonArray fields from/to BSON streams </summary>
		public class JsonArraySerializer : SerializerBase<JsonArray> {
			public override JsonArray Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
				return context.ReadJsonArray();
			}
			public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonArray value) {
				context.WriteJsonArray(value);
				
			}
		}
	}

	/// <summary> Class to hold some helpers for serializing and deserializing 
	/// directly to and from <see cref="JsonObject"/> / <see cref="JsonArray"/>. </summary>
	public static class BsonDeSerHelpers {

		/// <summary> Directly Serialize a <see cref="JsonObject"/></summary>
		/// <param name="ctx"> context </param>
		/// <param name="value"> Object to serialize </param>
		public static void WriteJsonObject(this BsonSerializationContext ctx, JsonObject value) {
			if (value == null) { 
				ctx.Writer.WriteNull(); 
				return;
			}
			//Log.Info($"Beginning Object");
			ctx.StartObject();
			foreach (var pair in value) {
				//Log.Info($"Writing {pair.Key}: ({pair.Value.JsonType})");
				ctx.Writer.WriteName(pair.Key);
				if (pair.Value == null) { ctx.Writer.WriteNull(); }
				else if (pair.Value.JsonType == JsonType.String) { ctx.Writer.WriteString(pair.Value.stringVal); }
				else if (pair.Value.JsonType == JsonType.Number) { ctx.Writer.WriteDouble(pair.Value.doubleVal); }
				else if (pair.Value.JsonType == JsonType.Object) { ctx.WriteJsonObject(pair.Value as JsonObject); }
				else if (pair.Value.JsonType == JsonType.Array) { ctx.WriteJsonArray(pair.Value as JsonArray); }
				else if (pair.Value.JsonType == JsonType.Boolean) { ctx.Writer.WriteBoolean(pair.Value.boolVal); }
				else if (pair.Value.JsonType == JsonType.Null) { ctx.Writer.WriteNull(); }
				
			}
			//Log.Info($"Finishing Object");
			ctx.EndObject();
		}

		/// <summary> Directly Serialize a <see cref="JsonArray"/></summary>
		/// <param name="ctx"> context </param>
		/// <param name="value"> Array to serialize </param>
		public static void WriteJsonArray(this BsonSerializationContext ctx, JsonArray value) {
			if (value == null) {
				ctx.Writer.WriteNull();
				return;
			}
			//Log.Info($"Beginning Array");
			ctx.StartArray();
			foreach (var item in value) {
				//Log.Info($"Writing {item.JsonType})");
				if (item == null) { ctx.Writer.WriteNull(); }
				else if (item.JsonType == JsonType.String) { ctx.Writer.WriteString(item.stringVal); }
				else if (item.JsonType == JsonType.Number) { ctx.Writer.WriteDouble(item.doubleVal); }
				else if (item.JsonType == JsonType.Object) { ctx.WriteJsonObject(item as JsonObject); }
				else if (item.JsonType == JsonType.Array) { ctx.WriteJsonArray(item as JsonArray); }
				else if (item.JsonType == JsonType.Boolean) { ctx.Writer.WriteBoolean(item.boolVal); }
				else if (item.JsonType == JsonType.Null) { ctx.Writer.WriteNull(); }
				
			}
			//Log.Info($"Finishing Object");
			ctx.EndArray();
		}
		
		/// <summary> Directly Deserialize a <see cref="JsonObject"/></summary>
		/// <param name="ctx"> context </param>
		public static JsonObject ReadJsonObject(this BsonDeserializationContext ctx) {
			var firstType = ctx.Reader.CurrentBsonType;
			if (firstType == BsonType.Null) {
				ctx.Reader.ReadNull();
				return null;
			}
			JsonObject value = new JsonObject();
			//Log.Info($"Starting Reading JsonObject");
			ctx.StartObject();
			
			//Log.Info($"Before loop, state is {ctx.Reader.State}");
			while (true) {
				// Why is the type before the name?
				// Log.Info($"Top of loop, state is {ctx.Reader.State}");
				var nextType = ctx.Reader.ReadBsonType();

				// This is retarded, it should know after reading the start
				// that the next thing is going to be the end, and shouldn't even need to enter the loop.
				if (nextType == BsonType.EndOfDocument) { break; }
				// Log.Info($"Read type is [{nextType}], state is {ctx.Reader.State}");
				var name = ctx.ReadName();
				// Log.Info($"Read name is \"{name}\", state is {ctx.Reader.State}");

				if (nextType == BsonType.String) { value[name] = ctx.Reader.ReadString(); }
				else if (nextType == BsonType.Int32) { value[name] = ctx.Reader.ReadInt32(); }
				else if (nextType == BsonType.Int64) { value[name] = ctx.Reader.ReadInt64(); }
				else if (nextType == BsonType.Double) { value[name] = ctx.Reader.ReadDouble(); }
				else if (nextType == BsonType.Document) { value[name] = ReadJsonObject(ctx); }
				else if (nextType == BsonType.Array) { value[name] = ReadJsonArray(ctx); }
				else if (nextType == BsonType.Boolean) { value[name] = ctx.Reader.ReadBoolean(); }
				else if (nextType == BsonType.Null) { value[name] = JsonNull.instance; ctx.Reader.ReadNull(); }
				else { ctx.Reader.SkipValue(); }

			}

			// Log.Info($"Finishing Reading JsonObject");
			ctx.EndObject();
			return value;
		}

		/// <summary> Directly Deserialize a <see cref="JsonArray"/></summary>
		/// <param name="ctx"> context </param>
		public static JsonArray ReadJsonArray(this BsonDeserializationContext ctx) {
			var firstType = ctx.Reader.CurrentBsonType;
			if (firstType == BsonType.Null) {
				ctx.Reader.ReadNull(); 
				return null;
			}
			JsonArray value = new JsonArray();
			// Log.Info($"Starting Reading JsonArray");
			ctx.StartArray();
			while (true) {
				// Log.Info($"Top of loop, state is {ctx.Reader.State}");
				var nextType = ctx.Reader.ReadBsonType();
				// This is retarded, again it should know immediately upon consuming
				// the start of the array that the array immediately ends
				// and shouldn't need to enter the loop. 
				if (nextType == BsonType.EndOfDocument) { break; }

				// Log.Info($"Read type is [{nextType}], state is {ctx.Reader.State}");
				if (nextType == BsonType.String) { value.Add(ctx.Reader.ReadString()); }
				else if (nextType == BsonType.Int32) { value.Add(ctx.Reader.ReadInt32()); }
				else if (nextType == BsonType.Int64) { value.Add(ctx.Reader.ReadInt64()); }
				else if (nextType == BsonType.Double) { value.Add(ctx.Reader.ReadDouble()); }
				else if (nextType == BsonType.Document) { value.Add(ctx.ReadJsonObject()); }
				else if (nextType == BsonType.Array) { value.Add(ctx.ReadJsonArray()); }
				else if (nextType == BsonType.Boolean) { value.Add(ctx.Reader.ReadBoolean()); }
				else if (nextType == BsonType.Null) { value.Add(JsonNull.instance); ctx.Reader.ReadNull(); }
				else { ctx.Reader.SkipValue(); }

			}

			// Log.Info($"Finishing Reading JsonArray");
			ctx.EndArray();
			return value;
		}

		/// <summary> Helper to read a name, since for some reason 
		/// the zero-parameter version is not a part of the <see cref="IBsonReader"/> interface,
		/// but is a part of the <see cref="BsonReader"/> class, 
		/// but all IBsonReaders are just BsonReaders anyway...
		/// Seems like a very inconvinent little oversight tbh. </summary>
		/// <param name="ctx"> Context </param>
		/// <returns> Name read from Bson stream </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ReadName(this BsonDeserializationContext ctx) {
			if (ctx.Reader is BsonReader) {
				return ((BsonReader)ctx.Reader).ReadName();
			}
			return ctx.Reader.ReadName(null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartObject(this BsonDeserializationContext ctx) { ctx.Reader.ReadStartDocument(); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndObject(this BsonDeserializationContext ctx) { ctx.Reader.ReadEndDocument(); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void StartObject(this BsonSerializationContext ctx) { ctx.Writer.WriteStartDocument(); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void EndObject(this BsonSerializationContext ctx) { ctx.Writer.WriteEndDocument(); }
	}
#else
	}
#endif




}
