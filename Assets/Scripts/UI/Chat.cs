using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon.Chat;
using UnityStandardAssets.Characters.FirstPerson;

public class Chat : MonoBehaviour, IChatClientListener {
	GameObject container;
	GameObject messageList;
	GameObject messageInput;
	Text message;
	ChatClient chatClient;
	string channelName;
	bool isTalking = false;

	void Start() {
		container = transform.Find("Wrapper").gameObject;
		messageList = container.transform.Find("MessageList").gameObject;
		message = messageList.transform.Find("Message").GetComponent<Text>();
		messageInput = transform.Find("MessageInput").gameObject;
	}

	void Update() {
		if(chatClient != null) {
			chatClient.Service();

			if(chatClient.State == ChatState.ConnectedToFrontEnd) {
				// If not talking and press Chat button, shows chat field.
				if(!isTalking && Input.GetButtonDown("Chat")) {
					ShowMessageInput();
					isTalking = true;
				}
				// If it's talking but pressed escape, close it.
				else if(isTalking && Input.GetButtonDown("Cancel")) {
					HideMessageInput();
					isTalking = false;
				}
				// If it's in talking and pressed enter, send message!
				else if(isTalking && Input.GetButtonDown("Submit")) {
					InputField messageInputField = messageInput.GetComponent<InputField>();

					// But if no message, just close.
					if(messageInputField.text == "") {
						HideMessageInput();
					}
					// Else, broadcast message!
					else {
						chatClient.PublishMessage(channelName, messageInputField.text);
						HideMessageInput();
					}

					isTalking = false;
				}
			}
		}
	}

	void ShowMessageInput() {
		// Lock user control
		if(Camera.main != null && Camera.main.transform.name != "LobbyCam") {
			Transform camParent = Camera.main.transform.parent;

			if(camParent) {
				FirstPersonController controller = camParent.GetComponent<FirstPersonController>();

				if(controller) {
					controller.IsActivated = false;
				}
			}
		}

		InputField messageInputField = messageInput.GetComponent<InputField>();
		messageInputField.text = "";

		messageInput.SetActive(true);

		StartCoroutine(CoGiveMessageFocus());
	}

	void HideMessageInput() {
		// Unlock user control
		if(Camera.main != null && Camera.main.transform.name != "LobbyCam") {
			Transform camParent = Camera.main.transform.parent;

			if(camParent) {
				FirstPersonController controller = camParent.GetComponent<FirstPersonController>();

				if(controller) {
					controller.IsActivated = true;
				}
			}
		}

		messageInput.SetActive(false);
	}

	IEnumerator CoGiveMessageFocus() {
		yield return new WaitForSeconds(.1f);

		InputField messageInputField = messageInput.GetComponent<InputField>();

		messageInputField.Select();
		messageInputField.ActivateInputField();
	}

	public void Connect(string version, string username, string channel) {
		if(string.IsNullOrEmpty(PhotonNetwork.PhotonServerSettings.ChatAppID)) {
			print("No ChatAppID provided");
			return;
		}

		message.text = "";

		channelName = channel;

		chatClient = new ChatClient(this);
		chatClient.Connect(PhotonNetwork.PhotonServerSettings.ChatAppID, version, new ExitGames.Client.Photon.Chat.AuthenticationValues(username));
	}

	public void Disconnect() {
		if(chatClient != null) {
			chatClient.Disconnect();
		}
	}

	public virtual void OnConnected() {
		container.SetActive(true);

		chatClient.Subscribe(new string[]{ channelName });
		chatClient.SetOnlineStatus(ChatUserStatus.Online);
	}

	public virtual void OnDisconnected() {
		container.SetActive(false);
	}

	public virtual void OnGetMessages(string channelName, string[] senders, object[] messages) {
		for(int i = 0; i < senders.Length; i++) {
			message.text = message.text + "\n"
								+ senders[i] + ": "
								+ messages[i];
		}

		// Scroll to bottom
		StartCoroutine(CoScrollToBottom());
	}

	IEnumerator CoScrollToBottom() {
		yield return new WaitForSeconds(.1f);
		messageList.GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 0);
	}

	public void OnPrivateMessage(string sender, object message, string channelName) { }

	public void OnSubscribed(string[] channels, bool[] results) {
		message.text = "Chat Online.";
		chatClient.PublishMessage(channelName, "Joined to channel " + channelName + ".");
	}

	public void OnUnsubscribed(string[] channels) { }

	public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }

	public void OnChatStateChange(ChatState state) { }

	public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message) {
		print(message);
	}

	void OnApplicationQuit() {
		Disconnect();
	}
}
