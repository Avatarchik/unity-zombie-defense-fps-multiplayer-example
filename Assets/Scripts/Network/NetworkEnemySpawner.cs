using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NetworkEnemySpawner : Photon.MonoBehaviour {
	[Header("Enemy Spawn Management")]
	public int maxZombies = 20;
	public float respawnDuration = 5.0f;
	public List<GameObject> spawnPoints = new List<GameObject>();
	
	[Header("Enemy Status")]
	public float startHealth = 100f;
	public float startMoveSpeed = 1f;
	public float startDamage = 15f;
	public int startEXP = 3;
	public int startFund = 4;
	public float upgradeDuration = 60f;	// Increase all enemy stats every 30 seconds

	private float upgradeTimer;
	[SerializeField] private float currentHealth;
	[SerializeField] private float currentMoveSpeed;
	[SerializeField] private float currentDamage;
	[SerializeField] private int currentEXP;
	[SerializeField] private int currentFund;

	private bool activate = true;
	private float spawnTimer;

	void OnMasterClientSwitched(PhotonPlayer newMasterClient) {
		if(PhotonNetwork.player == newMasterClient) {
			activate = true;
		}
	}

	void Start() {
		currentHealth = startHealth;
		currentMoveSpeed = startMoveSpeed;
		currentDamage = startDamage;
		currentEXP = startEXP;
		currentFund = startFund;
	}

	void Update() {
		if(!activate) return;

		if(spawnTimer < respawnDuration) {
			spawnTimer += Time.deltaTime;
		}
		else {
			SpawnEnemy();
		}

		if(upgradeTimer < upgradeDuration) {
			upgradeTimer += Time.deltaTime;
		}
		else {
			UpgradeEnemy();
		}
	}

	public void Begins() {
		// Reset Attributes
		currentHealth = startHealth;
		currentMoveSpeed = startMoveSpeed;
		currentDamage = startDamage;
		currentEXP = startEXP;
		currentFund = startFund;

		spawnTimer = 0;
		upgradeTimer = 0;

		if(PhotonNetwork.player != PhotonNetwork.masterClient) {
			activate = false;
		}
	}

	// Returns how many players in game
	int GetPlayerCount() {
		return PhotonNetwork.playerList.Length;
	}

	void SpawnEnemy() {
		if(spawnTimer < respawnDuration) return;

		int playerCount = GetPlayerCount();
		int spawnCount = 0;
		int maxSpawnCount = 5 + ((playerCount-1) * 1); 	// 1P: 5, 2P: 6, 3P: 7, 4P: 8
		int zombiesCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

		foreach(GameObject spawnPoint in spawnPoints) {
			// If zombies were spawned too many, just stop.
			if(zombiesCount >= maxZombies) break;

			// Check how many zombies are spawning once by player numbers
			else if(spawnCount >= maxSpawnCount) break;

			GameObject zombie = PhotonNetwork.Instantiate("Zombie", spawnPoint.transform.position, spawnPoint.transform.rotation, 0);

			float zombieHealth = currentHealth;
			float addtionalHP = currentHealth * 0.5f;

			zombieHealth = zombieHealth + (addtionalHP * (playerCount - 1));
			zombie.GetComponent<HealthManager>().SetHealth(currentHealth);

			KillReward killReward = zombie.GetComponent<KillReward>();
			killReward.SetReward(currentEXP, currentFund);

			// Boost rotating speed
			float rotateSpeed = 120f + currentMoveSpeed;
			rotateSpeed = Mathf.Max(rotateSpeed, 200f);	// Max 200f

			Chasing chasing = zombie.GetComponent<Chasing>();
			chasing.SetDamage(currentDamage);
			chasing.SetSpeed(currentMoveSpeed, rotateSpeed);

			spawnCount++;
			zombiesCount++;
		}
		
		spawnTimer = 0f;
	}

	void UpgradeEnemy() {
		currentHealth += 5;

		if(currentMoveSpeed < 5.5f) {
			currentMoveSpeed += 0.4f;
		}
		if(currentDamage < 90f) {
			currentDamage += 2f;
		}
		
		currentEXP++;
		currentFund++;

		upgradeTimer = 0;

		photonView.RPC("RPCSyncData", PhotonTargets.Others, currentHealth, currentMoveSpeed, currentDamage, currentEXP, currentFund);
	}

	[PunRPC]
	void RPCSyncData(float syncHealth, float syncMoveSpeed, float syncDamage, int syncExp, int syncFund) {
		currentHealth = syncHealth;
		currentMoveSpeed = syncMoveSpeed;
		currentDamage = syncDamage;
		currentEXP = syncExp;
		currentFund = syncFund;
	}
}
