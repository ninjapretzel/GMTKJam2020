using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using static GGUI;
using UnityEngine.UI;
using TMPro;
using LevelUpper.Extensions;
using Ex;

public class LoginGUI : GGUIBehaviour {

	public class LoginGUIService : Service {
		private LoginGUI gui;
		public void Bind(LoginGUI gui) {
			this.gui = gui;
		}
		public void On(LoginService.LoginFailure_Client fail) {
			gui.lastFailReason = fail.reason;
		}
		public void On(LoginService.LoginSuccess_Client succ) {
			gui.exDaemon.RunOnMainThread(() => {
				gui.SwitchTo(gui.nextScreen);
			});
		}
	}
	public Transform titleScreenObjects;
	public string title = "Defenders\nOf The\nPolyverse";
	public GGUIBehaviour nextScreen;
	public string lastFailReason = "";


	string user = "";
	string pass = "";

	LoginService loginService = null;
	ExDaemon exDaemon = null;

	GGUIControl userEntry;
	GGUIControl passEntry;
	GGUIControl submit;
	// Button button;
	// public MaterialPropertyAnimator titleTextAnimation;

	public override void OnEnable() {
		base.OnEnable();
		// button.onClick.RemoveAllListeners();
		exDaemon = GetComponent<ExDaemon>();
		exDaemon?.client?.AddService<LoginGUIService>().Bind(this);
		loginService = exDaemon.client.GetService<LoginService>();
		//button.onClick.AddListener()

	}

	public override void OnDisable() {
		base.OnDisable();

		exDaemon?.client?.RemoveService<LoginGUIService>();
		if (titleScreenObjects != null) {
			if (loginService.LoggedIn) {
				titleScreenObjects.gameObject.SetActive(false);
			}
		}
		loginService = null;

	}

	public override void Update() {
		base.Update();

		if (Input.GetKeyDown(KeyCode.Tab)) {
			if (userEntry != null && passEntry != null) {

				InputField userField = userEntry.liveObject?.GetComponent<InputField>();
				InputField passField = passEntry.liveObject?.GetComponent<InputField>();
				(userField.isFocused ? passField : userField)?.Focus();

			}
		}

	}

	public override void RenderGUI() {

		LoadSkin("Tech");


		//var titleControl = Text(new Rect(0, 0, 1, .33f), title);
		// titleControl.style = skin["title"];
		/*
		titleControl.OnReadyRect((rt) => {
			var pa = rt.gameObject.AddComponent<TextMeshProUGUIMaterialPropertyAnimator>();
			pa.floatProps = titleTextAnimation.floatProps;
			pa.colorProps = titleTextAnimation.colorProps;
			pa.vectorProps = titleTextAnimation.vectorProps;

		});
		//*/

		NestPanel(new Rect(.33f, .33f, .33f, .33f), () => {

			userEntry = TextField(new Rect(0, 0, 1, .33f), "Username", user, "Username",
				(str) => { user = str; },
				(str) => {
					passEntry.liveObject.GetComponent<InputField>().Focus();
				});

			userEntry.OnReadyRect((rt) => {
				rt.GetComponent<InputField>().Focus();
			});

			passEntry = PassField(new Rect(0, .33f, 1, .33f), "Password", pass, "password",
				(str) => { pass = str; },
				(str) => { submit.Clicked(); });

			submit = Button(new Rect(0, .66f, 1, .33f), "Login!", () => {

				//LoginModule login = NetworkDaemon.main.GetModule<LoginModule>();

				userEntry.liveObject.GetComponent<Image>().color = loginService.usernameValidator(user) ? Color.white : Color.red;
				passEntry.liveObject.GetComponent<Image>().color = loginService.passwordValidator(pass) ? Color.white : Color.red;

				loginService.Login_Slave(user, pass);

			});

		});


		var message = Text(new Rect(0, .66f, 1, .33f), "");
		message.Update((rt) => {
			var display = rt.GetComponent<TextMeshProUGUI>();

			if (loginService == null) {
				display.text = "Incorrect network service configuration.";

			} else if (loginService.server == null) {
				display.text = "Unconnected...";

			} else {
				display.text = lastFailReason;
			}

		});

	}

}

