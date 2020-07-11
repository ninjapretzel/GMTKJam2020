#if UNITY_2017 || UNITY_2018 || UNITY_2019 || UNITY_2020
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.Threading;
// For whatever reason, unity doesn't like mongodb, so we have to only include it server-side.
#if !UNITY
using MongoDB.Bson.Serialization.Attributes;
#endif
namespace Ex {

	/// <summary> Represents information about a logged in user. </summary>
	public class Credentials {
		/// <summary> Name of user </summary>
		public string username { get; private set; }
		/// <summary> Password hash </summary>
		public string passhash { get; private set; }
		/// <summary> User ID, equal to guid value of token if valid, otherwise <see cref="Guid.Empty"/> </summary>
		public Guid userId { get; private set; }
		/// <summary> Token provided by server, typically a valid <see cref="Guid"/></summary>
		public string token { get; private set; }
		/// <summary> Timestamp of creation </summary>
		public DateTime created { get; private set; }

		/// <summary> Used to create a credentials object on the client. </summary>
		/// <param name="user"> Username </param>
		/// <param name="token"> Guid session token as a string </param>
		public Credentials(string user, string token) {
			username = user;
			passhash = "local";
			Guid guid;
			this.token = token;
			if (!Guid.TryParse(token, out guid)) { guid = Guid.Empty; }
			userId = guid;
			created = DateTime.UtcNow;
		}
		/// <summary> Used to create a credentials object on the server, after authenticating the user. </summary>
		/// <param name="user"> Username </param>
		/// <param name="hash"> Password hash </param>
		public Credentials(string user, string hash, Guid userId) {
			username = user;
			passhash = hash;
			this.userId = userId;
			created = DateTime.UtcNow;
		}

	}

	public class LoginService : Service {

		#region MESSAGE_STRUCTS
		/// <summary> Message type sent on a client when a login attempt was successful </summary>
		public struct LoginSuccess_Client { public Credentials credentials; }
		/// <summary> Message type sent on a client when a login attempt was failed. </summary>
		public struct LoginFailure_Client { public string reason; }

		/// <summary> Message type sent on server when a login attempt was successful. </summary>
		public struct LoginSuccess_Server {
			public readonly Client client;
			public LoginSuccess_Server(Client client) { this.client = client; }
		}
		/// <summary> Message type sent on server when a login attempt was failed</summary>
		public struct LoginFailure_Server {
			public string ip;
			public string reason;
			public int sequence;
		}
		#endregion

#if !UNITY
		/// <summary> Database object storing user login info </summary>
		[BsonIgnoreExtraElements]
		public class UserLoginInfo : DBEntry {
			/// <summary> Username </summary>
			public string userName { get; set; }
			/// <summary> password hash </summary>
			public string hash { get; set; }
			/// <summary> last login </summary>
			public DateTime lastLogin { get; set; }
		}

		/// <summary> Database object storing user account creation info </summary>
		[BsonIgnoreExtraElements]
		public class UserAccountCreation : DBEntry {
			/// <summary> Account name </summary>
			public string userName { get; set; }
			/// <summary> IP Address </summary>
			public string ipAddress { get; set; }
			/// <summary> Time of account creation </summary>
			public DateTime time { get; set; }
		}

		/// <summary> Results of a login attempt </summary>
		public enum LoginResult : int {
			/// <summary> Login did not exist, was created, and user was logged in. </summary>
			Success_Created,
			/// <summary> Login did exist, and user was logged in </summary>
			Success,
			
			/// <summary> Login was rejected due to unspecified reasons </summary>
			Failed_Unspecified = 10000,
			/// <summary> Login was rejected due to version mismatch </summary>
			Failed_VersionMismatch,
			/// <summary> Login was rejected due to password mismatch </summary>
			Failed_BadCredentials,
			/// <summary> Login was rejected due to login cooldown applied </summary>
			Failed_LoginCooldown,
			/// <summary> Login was rejected due to creation cooldown applied </summary>
			Failed_CreationCooldown,
			/// <summary> Login was rejected due to username not valid </summary>
			Failed_BadUsername,
			/// <summary> Login was rejected due to client already being logged in </summary>
			Failed_ClientAlreadyLoggedIn,
			/// <summary> Login was rejected due to requested user already being logged in </summary>
			Failed_UserAlreadyLoggedIn,
		}

		/// <summary> Database object storing login attempts. </summary>
		[BsonIgnoreExtraElements]
		public class LoginAttempt : DBEntry {
			/// <summary> Username attempted for login </summary>
			public string userName { get; set; }
			/// <summary> Passhash provided for login </summary>
			public string hash { get; set; }
			/// <summary> Timestamp of login attempt </summary>
			public DateTime timestamp { get; set; }
			/// <summary> Was the login a success? </summary>
			public bool success { get; set; }
			/// <summary> Was the login an account creation? (implies success) </summary>
			public bool creation { get; set; }
			/// <summary> IP of client requesting login </summary>
			public string ip { get; set; }
			/// <summary> Enum result of login </summary>
			public LoginResult result { get; set; }
			/// <summary> Descriptive result of login </summary>
			public string result_desc { get; set; }
		}
		/// <summary> information about a logged in client </summary>
		public struct Session {
			public Credentials credentials { get; private set; }
			public Client client { get; private set; }
			public Session(Client client, Credentials credentials) {
				this.client = client;
				this.credentials = credentials;
			}
		}
		
		/// <summary> Serverside, maps client to user ID </summary>
		private Dictionary<Client, Session> loginsByClient;
		/// <summary> Serverside, maps user ID to client </summary>
		private Dictionary<Guid, Session> loginsByUserId;

		/// <summary> Gets a login by a user ID </summary>
		/// <param name="userId"> ID of user to check for </param>
		/// <returns> Login information for user, if they are currently logged in </returns>
		public Session? GetLogin(Guid userId) {
			if (isSlave) { return null; }
			if (loginsByUserId.ContainsKey(userId)) {
				return loginsByUserId[userId];
			}
			return null;
		}

		/// <summary> Gets a login by a client ID </summary>
		/// <param name="client"> Client to check for </param>
		/// <returns> Login information for client, if they are currently logged in. </returns>
		public Session? GetLogin(Client client) {
			if (isSlave) { return null; }
			if (loginsByClient.ContainsKey(client)) {
				return loginsByClient[client];
			}
			return null;
		}

#endif

		/// <summary> Reference to login information that a local client can use. passhash of this is always "local" if it exists. </summary>
		public Credentials localLogin { get; private set; } = null;

		/// <summary> Is the local Client logged in? </summary>
		public bool LoggedIn { get { return localLogin != null; } }
		/// <summary> Version associated with login endpoint (client and server) must match to allow logins. </summary>
		public string versionCode { get { return VersionInfo.VERSION; } }

		/// <summary> Function used to check for valid usernames. </summary>
		public Func<string, bool> usernameValidator = DefaultValidateUsername;
		/// <summary> Function used to check for valid passwords. </summary>
		public Func<string, bool> passwordValidator = DefaultValidatePassword;
		/// <summary> Hash function. Should be replaced with something more secure than the default. </summary>
		public Func<string, string> Hash = DefaultHash;

		/// <summary> Initializer for a newly logged in user </summary>
		public Action<Guid> userInitializer;

#if !UNITY
		/// <summary> Connected DBService </summary>
		DBService dbService;

		public override void OnStart() {
			dbService = GetService<DBService>();
		}

		public override void OnEnable() {
			loginsByClient = new Dictionary<Client, Session>();
			loginsByUserId = new Dictionary<Guid, Session>();
		}

		public override void OnDisable() {
			loginsByClient.Clear();
			loginsByUserId.Clear();
		}

		public override void OnConnected(Client client) {
			
		}
		
		public override void OnFinishedDisconnected(Client client) {
			if (loginsByClient.ContainsKey(client)) {
				Log.Verbose($"Logging out {client.identity}");
				Guid guid = loginsByClient[client].credentials.userId;
				
				loginsByClient.Remove(client);
				loginsByUserId.Remove(guid);
			}
			
		}
#endif

		private int _isAttemptingLogin;
		public bool isAttemptingLogin { get { return _isAttemptingLogin != 0; } }
		private string loginName;
		/// <summary> Begins a login for the given username/password pair </summary>
		/// <param name="user"> Username </param>
		/// <param name="pass"> Password </param>
		/// <returns> True, if the login is propagated to the server, false otherwise. (Login already in progress, already logged in, or called on a server instance) </returns>
		public bool Login_Slave(string user, string pass) {
			if (!isSlave) { return false; }

			if (localLogin != null) { return false; }
			if (isAttemptingLogin) { return false; }
			if (Interlocked.CompareExchange(ref _isAttemptingLogin, 1, 0) != 0) { return false; }
			
			loginName = pass;
			string hashedPass = Hash(pass);

			server.localClient.Call(Login, user, hashedPass, VersionInfo.VERSION);
			return true;
		}

		/// <summary> Server -> Client RPC. Response with results of login attempt. </summary>
		/// <param name="msg"> RPC Info. </param>
		public void LoginResponse(RPCMessage msg) {
			if (!isMaster) {
				Interlocked.Exchange(ref _isAttemptingLogin, 0);
				Log.Info($"LoginResponse: {msg[0]}, [{msg[1]}]");
				if (msg[0] == "succ") {
					localLogin = new Credentials(loginName, msg[1]);
					server.On(new LoginSuccess_Client() { credentials = localLogin } );
				} else {
					server.On(new LoginFailure_Client() { reason = msg[1] });
				}
			}	
		}

		string _VERSION_MISMATCH = null;
		string VERSION_MISMATCH {
			get {
				if (_VERSION_MISMATCH != null) { return _VERSION_MISMATCH; }
				return (_VERSION_MISMATCH = $"Version Mismatch\nPlease update to version [{versionCode}]");
			}
		}

		/// <summary> Connected database service </summary>
		
		/// <summary> Client -> Server RPC. Checks user and credentials to validate login, responds with <see cref="LoginResponse(RPCMessage)"/></summary>
		/// <param name="msg"> RPC Info. </param>
		public void Login(RPCMessage msg) {
#if !UNITY
			string user = msg[0];
			string hash = msg[1];
			string version = msg.numArgs >= 3 ? msg[2] : "[[Version Not Set]]";
			// Login flow.
			Credentials creds = null;
			LoginResult result = LoginResult.Failed_Unspecified;
			string reason = "none";
			UserLoginInfo userInfo = null;

			#region Submethod CheckLogin()
			{ // SubMethod: CheckLogin()
				if (version != versionCode) {
					Log.Debug($"{nameof(LoginService)}: Version mismatch {version}, expected {versionCode}");
					reason = VERSION_MISMATCH;
					result = LoginResult.Failed_VersionMismatch;
				} else if (!usernameValidator(user)) {
					Log.Debug($"{nameof(LoginService)}: Bad username {user}");
					reason = "Invalid Username";
					result = LoginResult.Failed_BadUsername;
				} else if (GetLogin(msg.sender) != null) {
					Log.Debug($"{nameof(LoginService)}: Client {msg.sender.identity} already logged in");
					reason = "Already Logged In";
					result = LoginResult.Failed_ClientAlreadyLoggedIn;
				} else {
					userInfo = dbService.Get<UserLoginInfo>(nameof(userInfo.userName), user);

					if (userInfo == null) {
						Log.Debug($"{nameof(LoginService)}: User {user} not found, creating them now. ");
						
						Guid userId = Guid.NewGuid();
						userInfo = CreateNewUser(msg);

						if (userInfo != null) {
							result = LoginResult.Success_Created;
							creds = new Credentials(user, hash, userInfo.guid);
						} else {
							result = LoginResult.Failed_CreationCooldown;
							reason = "Too many account creations";
						}

						

					} else {
						// Check credentials against existing credentials.
						if (loginsByUserId.ContainsKey(userInfo.guid)) {
							reason = "Already logged in";
							result = LoginResult.Failed_UserAlreadyLoggedIn;
						} else if (hash != userInfo.hash) {
							reason = "Bad credentials";
							result = LoginResult.Failed_BadCredentials;
						} else {
							creds = new Credentials(user, hash, userInfo.guid);
							result = LoginResult.Success;
						}
					

					}
				}
			} // Submethod: CheckLogin()
			#endregion

			#region Submethod RecordLoginAttempt
			{ // SubMethod: RecordLoginAttempt()
				LoginAttempt attempt = new LoginAttempt();
				attempt.result = result;
				attempt.result_desc = result.ToString();
				attempt.timestamp = DateTime.UtcNow;
				attempt.hash = hash;
				attempt.userName = user;
				attempt.ip = msg.sender.remoteIP;
				if (userInfo != null) {
					attempt.success = true;
					attempt.creation = result == LoginResult.Success_Created;
					attempt.guid = userInfo.guid;
				} else {
					attempt.success = attempt.creation = false;
					attempt.guid = Guid.Empty;
				}

				dbService.Save(attempt);
			} // Submethod: RecordLoginAttempt()
			#endregion

			#region Submethod Respond
			{// Submethod: Respond()
				if (creds == null) {
					msg.sender.Call(LoginResponse, "fail", reason);

					Log.Info($"Client {msg.sender.identity} Failed to login.");

					server.On(new LoginFailure_Server() {
						ip = msg.sender.remoteIP
					});

				} else {
					var session = new Session(msg.sender, creds);
					loginsByClient[msg.sender] = session;
					loginsByUserId[creds.userId] = session;

					userInfo.lastLogin = DateTime.UtcNow;
					dbService.Save(userInfo);

					msg.sender.Call(LoginResponse, "succ", creds.userId);

					Log.Info($"Client {msg.sender.identity} logged in as user {creds.username} / {creds.userId}. ");

					server.On(new LoginSuccess_Server(msg.sender));
				}

			} // Submethod: Respond()
			#endregion
#endif
		}
		
#if !UNITY
		private UserLoginInfo CreateNewUser(RPCMessage msg) {
			string user = msg[0];
			string hash = msg[1];

			List<UserAccountCreation> accountCreations = dbService.GetAll<UserAccountCreation>(nameof(UserAccountCreation.ipAddress), msg.sender.remoteIP);
			Log.Debug($"Client at {msg.sender.remoteIP} has {accountCreations.Count} account creations.");
			if (accountCreations.Count > 5) {
				return null;
			}

			Guid userId = Guid.NewGuid();
			UserLoginInfo userInfo = new UserLoginInfo();
			userInfo.userName = user;
			userInfo.hash = hash;
			userInfo.guid = userId;
			userInfo.lastLogin = DateTime.UtcNow;
			dbService.Save(userInfo);

			try {
				userInitializer?.Invoke(userId);

				dbService.Save(new UserAccountCreation() {
					userName = user,
					guid = userId,
					ipAddress = msg.sender.remoteIP,
					time = DateTime.UtcNow
				});

			} catch (Exception e) {
				Log.Error($"Error initializing user {user} / {userId} ", e);
				return null;
			}

			return userInfo;
		}
#endif


		/// <summary> Simple quick check for valid usernames. </summary>
		/// <param name="user"> Name to check </param>
		/// <returns> True if name is between 4 and 64 characters long, and contains only [a-zA-Z_0-9] characters, and does not start with [0-9] </returns>
		public static bool DefaultValidateUsername(string user) {
			if (user == null) { return false; }
			if (user.Length < 4 || user.Length > 64) { return false; }

			for (int i = 0; i < user.Length; i++) {
				char c = user[i];
				if (c > 'z') { return false; } // No characters past 'z' allowed, narrows down valid usernames quick.
				if (c >= 'a') { continue; } // Accept a-z
				if (c > 'Z') { return false; } // Next range is A-Z, reject characters between 'Z' and 'a'.
				if (c >= 'A') { continue; }
				if (c == '_') { continue; }

				// must start with [a-zA-Z_]
				if (i > 0 && c >= '0' && c <= '9') { continue; }

				// Reject any other characters
				return false;
			}

			// If we've passed the whole string without rejecting, it's good.
			return true;
		}

		/// <summary> Simple check for valid passwords. </summary>
		/// <param name="pass"> password to check </param>
		/// <returns> True, if password is at least 8 characters. That's all we care about for simplicity's sake. </returns>
		public static bool DefaultValidatePassword(string pass) {
			if (pass.Length < 4) { return false; }

			return true;
		}
		/// <summary> Default Password 'Hash' function. </summary>
		/// <param name="pass"> password to hash </param>
		/// <returns> Simple salted + hashed result. </returns>
		public static string DefaultHash(string pass) {
			return ":\\:" + (pass + "IM A STUPID HACKER DECOMPILING YOUR CODE LOL").GetHashCode() + ":/:";
		}

	}

}
