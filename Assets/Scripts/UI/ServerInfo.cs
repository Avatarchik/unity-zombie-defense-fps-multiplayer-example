using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public delegate void CallbackFunc();

public class ServerInfo : MonoBehaviour {
	Text infoText;
	Button joinButton;
	CallbackFunc callbackFunc;
	public string serverName = "";
	public int players = 0;
	public int maxPlayers = 0;

	public CallbackFunc onJoinButtonClick {
		get {
			return callbackFunc;
		}
		set {
			callbackFunc = value;
		}
	}

	void Awake() {
		infoText = transform.Find("ServerInfoText").GetComponent<Text>();
		joinButton = transform.Find("JoinButton").GetComponent<Button>();

		joinButton.onClick.AddListener(() => {
			if(callbackFunc != null) callbackFunc();
		});
	}

	void OnGUI() {
		infoText.text = serverName + " (" + players + "/" + maxPlayers + ")";

		if(players >= maxPlayers) {
			joinButton.interactable = false;
		}
		else {
			joinButton.interactable = true;
		}
	}
}
