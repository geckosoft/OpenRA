using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using OpenRA.FileFormats;
using OpenRA.Support;

namespace OpenRA.Server
{
	public class UserSession : MasterResult
	{
		public class InformSessionResult : MasterResult
		{
			public string EncryptedUserId;
		}
		#region Password encryption key / iv (shared with master)
		private const string KEY = "SeR59On5ArY58qW7n31jDzwrjbQpPLub";
		private const string IV = "Jp9638NtUHK2C9dy";
		#endregion

		public int UserId;
		public string UserKey = "";
		public string Username = "";

		public UserSession()
		{
			OnAuthenticated = (success) => { };
		}

		internal Exception Exception { get; set; }

		public Action<bool> OnAuthenticated { get; set; }

		public void Authenticate(string username, string password)
		{
			Action a = () =>
			           {
			           	try
			           	{
			           		Exception = null;

			           		string url = Game.Settings.Server.MasterServer + "auth.php?do=login";

			           		string passwordEncrypted = Encryption.Encrypt(password, KEY, IV);

			           		// Assign the post-data
			           		var data = new NameValueCollection();
			           		data["username"] = username;
			           		data["password"] = passwordEncrypted;

			           		// Perform the post
			           		string reply = HttpClient.Post(url, data);

			           		// Attempt to load the yaml
			           		List<MiniYamlNode> yaml = MiniYaml.FromString(reply);

			           		// Attempt to apply the yaml onto the current class
			           		FieldLoader.Load(this, yaml[0].Value);

			           		OnAuthenticated(Success);
			           	}
			           	catch (Exception ex)
			           	{
			           		Exception = ex;
			           		OnAuthenticated(false);
			           	}
			           };

			a.BeginInvoke(null, null);
		}

		public bool IsBusy { get; internal set; }

		public bool Authenticated
		{
			get { return Success; }
		}

		public void Inform(int gameId, Action<bool, InformSessionResult> done)
		{
			if (IsBusy)
				return;
			IsBusy = true;
			Action a = () =>
			{
				try
				{
					Exception = null;

					string url = Game.Settings.Server.MasterServer + "auth.php?do=inform";

					// Assign the post-data
					var data = new NameValueCollection();
					data["user_id"] = UserId + "";
					data["user_key"] = UserKey;
					data["game_id"] = gameId + "";
 
					// Perform the post
					string reply = HttpClient.Post(url, data);

					// Attempt to load the yaml
					List<MiniYamlNode> yaml = MiniYaml.FromString(reply);

					// Attempt to apply the yaml onto the current class
					var result = FieldLoader.Load <InformSessionResult>(yaml[0].Value);

					if (!result.Success)
					{
						done(false, result);
					}

					done(true, result);
				}
				catch (Exception ex)
				{
					Exception = ex;
					done(false, null);
				}
				IsBusy = false;
			};

			a.BeginInvoke(null, null);
		}
	}
}