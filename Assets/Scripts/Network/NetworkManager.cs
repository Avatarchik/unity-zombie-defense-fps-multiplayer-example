using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;

public enum GameState {
	WAITING,
	PLAYING
};

public class NetworkManager : Photon.MonoBehaviour {
	[SerializeField] string version = "v0.0.1";
	[SerializeField] string playerName = "Player";

	[Header("Musics")]
	public AudioClip mainBGM;
	public AudioClip roundBGM;
	public AudioClip gameOverBGM;

	[Header("Sounds")]
	public AudioClip deadNoticeSound;


	[Header("UI Prefabs")]
	public GameObject serverPrefab;

	[Header("UI Refs")]
	public GameObject loginUI;
	public GameObject registerUI;
	public GameObject mainUI;
	public GameObject browseServerUI;
	public GameObject createServerUI;
	public GameObject waitingRoomUI;
	public GameObject enteringUI;
	public GameObject inGameUI;
	public GameObject playerUI;
	public GameObject inspectorUI;
	public GameObject deadScreen;
	public GameObject lobbyCam;
	public Text statusText;

	[Header("Chat Refs")]
	public Chat chat;

	[Header("Game Management")]
	public Transform spawnPoint;
	public GameObject enemySpawner;

	private string serverName = "";

	GlobalSoundManager globalSoundManager;
	NetworkPlayerStatus playerStatus;

	GameObject browseServerLoadingText;
	GameObject serverList;
	Button browseServerButton;
	Button createServerButton;
	Button logoutButton;
	GameObject player;

	int savedFund = 0;
	public int spawnedPlayers = 0;
	string joinedServerName;

	[SerializeField] GameState gameState = GameState.WAITING;
	[SerializeField] int alives = 0;

	public void SyncUserSpawned() {
		photonView.RPC("RPCSyncUserSpawned", PhotonTargets.MasterClient);
	}

	[PunRPC]
	void RPCSyncUserSpawned() {
		spawnedPlayers++;
		alives++;

		if(spawnedPlayers == PhotonNetwork.playerList.Length) {
			RefreshPlayerIndicators();
		}

		photonView.RPC("RPCSyncAlives", PhotonTargets.Others, alives);
	}

	[PunRPC]
	void RPCSyncAlives(int alives) {
		this.alives = alives;
	}

	void RefreshPlayerIndicators() {
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
			
		foreach(GameObject player in players) {
			player.GetComponent<Player>().StartCreateIndicators();
		}
	}

	void Awake() {
		globalSoundManager = transform.Find("GlobalSoundManager").GetComponent<GlobalSoundManager>();
		playerStatus = GetComponent<NetworkPlayerStatus>();

		PhotonNetwork.autoJoinLobby = true;

		HandleMainUIEvents();
		HandleBrowseServerUIEvents();
		HandleCreateServerUIEvents();
		HandleWaitingRoomUIEvents();

		loginUI.SetActive(true);
	}

	void Start() {
		globalSoundManager.PlayMusic(mainBGM);
	}

	void Update() {
		if(statusText != null && statusText.gameObject.activeSelf) {
			statusText.text = PhotonNetwork.connectionStateDetailed.ToString();		
		}
	}

	void StartGame() {
		PhotonNetwork.room.IsVisible = false;
		PhotonNetwork.room.IsOpen = false;

		photonView.RPC("RPCStartGame", PhotonTargets.All);
	}

	public void ShowPlayerDead(string name) {
		if(alives > 0) {
			StartCoroutine(CoShowPlayerDeadNotice(name));
		}
	}

	IEnumerator CoShowPlayerDeadNotice(string playerName) {
		globalSoundManager.Play(deadNoticeSound);

		GameObject playerDeadNotice = GameObject.Find("UI/InGameUI/PlayerUI/PlayerDeadNotice");
		playerDeadNotice.GetComponent<Text>().text = playerName + " was eaten alive by Zombies.";
		playerDeadNotice.SetActive(true);

		yield return new WaitForSeconds(5f);
		playerDeadNotice.SetActive(false);
	}

	public void SaveFund(int amount) {
		savedFund = amount;
	}

	public void RevivePlayers() {
		RefreshPlayerIndicators();
		photonView.RPC("RPCRevivePlayers", PhotonTargets.Others);
	}

	[PunRPC]
	void RPCRevivePlayers() {
		if(player == null) {
			Destroy(GameObject.FindWithTag("Inspector"));

			inspectorUI.SetActive(false);
			playerUI.SetActive(true);

			savedFund += (PhotonNetwork.playerList.Length * 100);

			player = PhotonNetwork.Instantiate(playerName, spawnPoint.position, spawnPoint.rotation, 0);
			player.GetComponent<FundSystem>().AddFund(savedFund);

			NetworkPlayerStatus playerStatus = GetComponent<NetworkPlayerStatus>();
			LevelSystem levelSystem = player.GetComponent<LevelSystem>();
			levelSystem.exp = playerStatus.exp;
			levelSystem.requireExp = playerStatus.rexp;
			levelSystem.level = playerStatus.level;

			savedFund = 0;

			StartCoroutine(CoActivateRespawnProtection(player.GetComponent<HealthManager>()));
		}

		RefreshPlayerIndicators();
	}

	IEnumerator CoActivateRespawnProtection(HealthManager health) {
		health.SetDamageFactor(0f);

		yield return new WaitForSeconds(20f);

		health.SetDamageFactor(1f);	
	}

	public void CheckPlayerDead() {
		photonView.RPC("RPCCheckPlayerDead", PhotonTargets.All);
	}

	[PunRPC]
	void RPCCheckPlayerDead() {
		alives--;

		if(PhotonNetwork.isMasterClient) {
			photonView.RPC("RPCSyncAlives", PhotonTargets.Others, alives);
			CheckGameOver();
		}
	}

	[PunRPC]
	void RPCStartGame() {
		ResetInGameUI();

		waitingRoomUI.SetActive(false);
		lobbyCam.SetActive(false);
		player = PhotonNetwork.Instantiate(playerName, spawnPoint.position, spawnPoint.rotation, 0);
		player.GetComponent<NetworkPlayer>().playerName = GetComponent<NetworkPlayerStatus>().id;

		NetworkPlayerStatus playerStatus = GetComponent<NetworkPlayerStatus>();
		LevelSystem levelSystem = player.GetComponent<LevelSystem>();
		levelSystem.exp = playerStatus.exp;
		levelSystem.requireExp = playerStatus.rexp;
		levelSystem.level = playerStatus.level;

		inGameUI.SetActive(true);
		enemySpawner.SetActive(true);
		enemySpawner.GetComponent<NetworkEnemySpawner>().Begins();
		playerUI.SetActive(true);
		playerUI.transform.Find("Startup").gameObject.SetActive(true);

		globalSoundManager.PlayMusic(roundBGM);

		gameState = GameState.PLAYING;
	}

	public void CheckGameOver() {
		if(gameState == GameState.WAITING) return;

		if(alives <= 0) {
			GameOver();
		}
	}

	void GameOver() {
		photonView.RPC("RPCGameOver", PhotonTargets.All);
	}

	[PunRPC]
	void RPCGameOver() {
		globalSoundManager.PlayMusic(gameOverBGM);

		deadScreen.SetActive(true);
		StartCoroutine(ShowDeadScreenAndCleanup());

		GetComponent<NetworkPlayerStatus>().UpdateData((string errorMessage) => {
			if(errorMessage != null) {
				print(errorMessage);
			}
		});

		gameState = GameState.WAITING;
	}
	
	IEnumerator ShowDeadScreenAndCleanup() {
		deadScreen.SetActive(true);

		Image image = deadScreen.GetComponent<Image>();
		Color origColor = image.color;

		for(float alpha = 0.0f; alpha <= 1.1f; alpha += 0.1f) {
			image.color = new Color(origColor.r, origColor.g, origColor.b, alpha);
			yield return new WaitForSeconds(0.1f);
		}
		
		yield return new WaitForSeconds(10f);		// Just wait 10 seconds more to reset

		yield return StartCoroutine(CleanupGame());
		yield break;
	}

	IEnumerator CleanupGame() {
		// Stop Enemyspawner works
		enemySpawner.SetActive(false);

		// Remove all zombies and players
		if(PhotonNetwork.isMasterClient) {
			PhotonNetwork.DestroyAll();
		}

		// Remove inspectors
		GameObject inspector = GameObject.FindWithTag("Inspector");
		if(inspector != null) Destroy(inspector);

		// Remove bodies
		GameObject[] bodies = GameObject.FindGameObjectsWithTag("Body");
		foreach(GameObject body in bodies) {
			Destroy(body);
		}

		alives = 0;
		spawnedPlayers = 0;
		ResetWaitingRoomUI();
		
		globalSoundManager.PlayMusic(mainBGM);

		yield break;
	}

	void ResetWaitingRoomUI() {
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		lobbyCam.SetActive(true);
		inGameUI.SetActive(false);
		waitingRoomUI.SetActive(true);

		if(PhotonNetwork.player == PhotonNetwork.masterClient) {
			waitingRoomUI.transform.Find("StartButton").gameObject.SetActive(true);
		}
		else {
			waitingRoomUI.transform.Find("StartButton").gameObject.SetActive(false);
		}

		UpdatePlayerCount();
	}

	void ResetInGameUI() {
		inGameUI.transform.Find("DeadScreen").gameObject.SetActive(false);
		inGameUI.transform.Find("InspectorUI").gameObject.SetActive(false);
		inGameUI.transform.Find("PlayerUI").gameObject.SetActive(true);
		inGameUI.transform.Find("InspectorUI").gameObject.SetActive(false);
	}

	void HandleMainUIEvents() {
		browseServerButton = mainUI.transform.Find("BrowseServerButton").GetComponent<Button>();
		createServerButton = mainUI.transform.Find("CreateServerButton").GetComponent<Button>();
		logoutButton = mainUI.transform.Find("ExitButton").GetComponent<Button>();

		browseServerButton.onClick.AddListener(() => {
			browseServerButton.interactable = false;
			createServerButton.interactable = false;

			PhotonNetwork.ConnectUsingSettings(version);
		});

		createServerButton.onClick.AddListener(() => {
			mainUI.SetActive(false);
			createServerUI.SetActive(true);
		});

		logoutButton.onClick.AddListener(() => {
			browseServerButton.interactable = false;
			createServerButton.interactable = false;
			logoutButton.interactable = false;

			Logout();
		});
	}

	void Logout() {
		StartCoroutine(CoLogout());
	}

	IEnumerator CoLogout() {
		NetworkPlayerStatus playerStatus = GetComponent<NetworkPlayerStatus>();

		WWWForm formData = new WWWForm();
		formData.AddField("_method", "delete");
		WWW httpResult = new WWW("http://YOUR_DOMAIN_HERE/auth?token=" + playerStatus.token, formData);

		yield return httpResult;
		
		playerStatus.token = "";
		playerStatus.id = "";

		GameObject.Find("UI/Lobby/PlayerID").GetComponent<Text>().text = "PlayerID: none";

		mainUI.SetActive(false);
		loginUI.SetActive(true);

		browseServerButton.interactable = true;
		createServerButton.interactable = true;
		logoutButton.interactable = true;
	}

	void HandleBrowseServerUIEvents() {
		browseServerLoadingText = browseServerUI.transform.Find("LoadingText").gameObject;
		serverList = browseServerUI.transform.Find("UIServerList").gameObject;
		
		browseServerUI.transform.Find("ExitButton").GetComponent<Button>().onClick.AddListener(() => {
			PhotonNetwork.Disconnect();
			browseServerUI.SetActive(false);

			mainUI.SetActive(true);
		});
	}

	void HandleCreateServerUIEvents() {
		Button submitButton = createServerUI.transform.Find("ServerForm/SubmitButton").GetComponent<Button>();
		Button cancelButton = createServerUI.transform.Find("ServerForm/CancelButton").GetComponent<Button>();
		InputField nameText = createServerUI.transform.Find("ServerForm/NameText").GetComponent<InputField>();

		submitButton.onClick.AddListener(() => {
			if(nameText.text == "" || nameText.text == "Server Name must be typed.") {
				nameText.text = "Server Name must be typed.";
				return;
			}

			submitButton.interactable = false;
			cancelButton.interactable = false;
			nameText.interactable = false;

			serverName = nameText.text;
			PhotonNetwork.ConnectUsingSettings(version);
		});

		cancelButton.onClick.AddListener(() => {
			createServerUI.SetActive(false);
			mainUI.SetActive(true);
		});
	}

	void HandleWaitingRoomUIEvents() {
		waitingRoomUI.transform.Find("StartButton").GetComponent<Button>().onClick.AddListener(() => {
			StartGame();
		});

		waitingRoomUI.transform.Find("ExitButton").GetComponent<Button>().onClick.AddListener(() => {
			serverName = "";

			PhotonNetwork.LeaveRoom();
			waitingRoomUI.SetActive(false);
			serverList.SetActive(true);
		});
	}

	void ResetServerList() {
		foreach(Transform child in serverList.transform) {
			Destroy(child.gameObject);
		}
	}

	void UpdatePlayerCount() {
		waitingRoomUI.transform.Find("PlayersText").GetComponent<Text>().text = PhotonNetwork.playerList.Length + " Player(s) is ready to play.";
	}

	public virtual void OnJoinedLobby() {
		if(serverName != "") {
			createServerUI.SetActive(false);
			enteringUI.SetActive(true);

			RoomOptions roomOptions = new RoomOptions() { IsVisible = true, MaxPlayers = 4 };
			PhotonNetwork.JoinOrCreateRoom(serverName, roomOptions, TypedLobby.Default);
		}
		else {
			mainUI.SetActive(false);
			browseServerUI.SetActive(true);
			browseServerLoadingText.SetActive(true);
		}
	}

	public virtual void OnJoinedRoom() {
		if(PhotonNetwork.isMasterClient) {
			waitingRoomUI.transform.Find("StartButton").gameObject.SetActive(true);
		}
		else {
			waitingRoomUI.transform.Find("StartButton").gameObject.SetActive(false);
		}

		enteringUI.SetActive(false);
		waitingRoomUI.SetActive(true);

		UpdatePlayerCount();

		string channelName;

		if(serverName != "") { 
			channelName = serverName;
		}
		else {
			channelName = joinedServerName;
		}

		chat.Connect(version, playerStatus.id, channelName);
	}

	public virtual void OnLeftRoom() {
		chat.Disconnect();
	}

	public virtual void OnLeftLobby() {
		Button submitButton = createServerUI.transform.Find("ServerForm/SubmitButton").GetComponent<Button>();
		Button cancelButton = createServerUI.transform.Find("ServerForm/CancelButton").GetComponent<Button>();
		InputField nameText = createServerUI.transform.Find("ServerForm/NameText").GetComponent<InputField>();

		submitButton.interactable = true;
		cancelButton.interactable = true;
		nameText.interactable = true;
		nameText.text = "";
	}

	public virtual void OnPhotonPlayerConnected (PhotonPlayer newPlayer) {
		UpdatePlayerCount();
	}

	public virtual void OnPhotonPlayerDisconnected (PhotonPlayer otherPlayer) {
		if(gameState == GameState.PLAYING) {
			if(PhotonNetwork.isMasterClient) {
				// Count how many actual player gameobjects in the scene
				// This is because there is no way to figure out that disconnected user was alive or not.
				StartCoroutine(CheckGameOverByCountPlayers());
			}
		}
		else {
			ResetWaitingRoomUI();
		}
	}

	IEnumerator CheckGameOverByCountPlayers() {
		// Yeah, 1f is not accurate value, but in test case, it's ok.
		// Maybe you should check every player's status in master client so that you can catch exact game state like how many players in
		yield return new WaitForSeconds(1f);

		alives = GameObject.FindGameObjectsWithTag("Player").Length;
		photonView.RPC("RPCSyncAlives", PhotonTargets.Others, alives);

		CheckGameOver();
	}

	public virtual void OnDisconnectedFromPhoton() {
		browseServerButton.interactable = true;
		createServerButton.interactable = true;
	}

	public virtual void OnReceivedRoomListUpdate() {
		browseServerLoadingText.SetActive(false);
		ResetServerList();

		RoomInfo[] roomInfos = PhotonNetwork.GetRoomList();

		foreach(RoomInfo roomInfo in roomInfos) {
			GameObject server = Instantiate(serverPrefab, Vector3.zero, Quaternion.identity);
			server.transform.parent = serverList.transform;

			ServerInfo serverInfo = server.GetComponent<ServerInfo>();
			serverInfo.serverName = roomInfo.Name;
			serverInfo.players = roomInfo.PlayerCount;
			serverInfo.maxPlayers = roomInfo.MaxPlayers;
			serverInfo.onJoinButtonClick = () => {
				joinedServerName = serverInfo.serverName;

				browseServerUI.SetActive(false);
				enteringUI.SetActive(true);

				PhotonNetwork.JoinRoom(serverInfo.serverName);
			};
		}
	}
	
	public virtual void OnConnectionFail(DisconnectCause cause) {
		print(cause.ToString());
	}
}
