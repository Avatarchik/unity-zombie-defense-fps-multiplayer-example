using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;

public class Player : Photon.MonoBehaviour {
	public GameObject inspectorPrefab;
	public GameObject indicatorUIPrefab;
	public AudioClip deathSound;

	CharacterController controller;
	HealthManager healthManager;
	Transform character;
	Transform rootRig;
	NetworkManager networkManager;
	AudioSource audioSource;
	bool isDestroyed = false;

	public int upgradeHealth = 0;
	public int upgradeRegeneration = 0;

	void Awake() {
		controller = GetComponent<CharacterController>();
		healthManager = GetComponent<HealthManager>();
		character = transform.Find("Character");
		rootRig = character.Find("Healthmale/Root");
		networkManager = GameObject.Find("GameManager").GetComponent<NetworkManager>();
	}
	
	void Start() {
		DeactivateRagdoll();
		
		if(photonView.isMine) networkManager.SyncUserSpawned();
	}

	void Update() {
		if(healthManager.IsDead && !isDestroyed) {
			isDestroyed = true;

			if(photonView.isMine) {
				DestroyAllIndicators();

				FundSystem fundSystem = GetComponent<FundSystem>();
				Transform fpsChar = transform.Find("FirstPersonCharacter");
				Transform weaponHolder = fpsChar.Find("WeaponHolder");
				int weaponsCount = weaponHolder.childCount;
				int savingFund;
				float totalFund = fundSystem.GetFund();

				// Accumulate upgraded costs
				for(int i = 0; i < weaponsCount; i++) {
					Transform weapon = weaponHolder.GetChild(i);
					WeaponBase weaponBase = weapon.GetComponent<WeaponBase>();
					int upgrades = weaponBase.GetUpgrades();
					int upgradeCost = weaponBase.upgradeCost;
					totalFund += (upgradeCost * upgrades);
				}

				// Accumulate other upgraded costs
				// Upgrade Health
				if(upgradeHealth > 1) {
					int cost = 100 + ((upgradeHealth - 1) * 75);
					totalFund += cost;
				}

				// Upgrade Regeneration
				if(upgradeRegeneration > 1) {
					int cost = 100 + ((upgradeRegeneration - 1) * 75);
					totalFund += cost;
				}

				totalFund *= 0.9f;
				savingFund = System.Convert.ToInt32(totalFund);

				networkManager.SaveFund(savingFund);
				
				fpsChar.gameObject.SetActive(false);
				character.gameObject.SetActive(true);
				
				GameObject.Find("UI/InGameUI/PlayerUI").SetActive(false);
				GameObject.Find("UI/InGameUI/InspectorUI").SetActive(true);

				DeployInspector();

				networkManager.CheckPlayerDead();

				photonView.RPC("RPCDeployBody", PhotonTargets.All);	
			}
		}
	}

	public void StartCreateIndicators() {
		photonView.RPC("RPCStartCreateIndicators", PhotonTargets.All);
	}

	[PunRPC]
	void RPCStartCreateIndicators() {
		CreatePlayerIndicators();
	}

	void DestroyAllIndicators() {
		GameObject[] indicators = GameObject.FindGameObjectsWithTag("AllyIndicator");

		foreach(GameObject indicator in indicators) {
			indicator.GetComponent<AllyIndicator>().SetTarget(null);
		}
	}

	public void CreatePlayerIndicators() {
		if(!photonView.isMine) return;

		DestroyAllIndicators();

		GameObject playerUI = GameObject.Find("UI/InGameUI/PlayerUI");
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
	
		foreach(GameObject player in players) {
			if(player == gameObject) continue;

			GameObject indicator = Instantiate(indicatorUIPrefab, Vector3.zero, Quaternion.identity);
			indicator.GetComponent<AllyIndicator>().SetTarget(player);
			indicator.transform.parent = playerUI.transform;
		}
	}

	void DeployInspector() {
		Vector3 spawnPosition = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z - 1);
		Instantiate(inspectorPrefab, spawnPosition, transform.rotation);
	}

	[PunRPC]
	void RPCDeployBody() {
		character.gameObject.SetActive(true);
		networkManager.ShowPlayerDead(GetComponent<NetworkPlayer>().playerName);

		character.GetComponent<SoundManager>().Play(deathSound);
		character.SetParent(null);
		ActivateRagdoll();

		if(photonView.isMine) PhotonNetwork.Destroy(gameObject);
	}

	void ActivateRagdoll() {
		controller.enabled = false;
		character.GetComponent<Animator>().enabled = false;

		// Hide weapons
		character.Find("Glock").GetComponent<SkinnedMeshRenderer>().enabled = false;
		character.Find("Colt Python").GetComponent<SkinnedMeshRenderer>().enabled = false;
		character.Find("MP5K").GetComponent<SkinnedMeshRenderer>().enabled = false;
		character.Find("UMP45").GetComponent<SkinnedMeshRenderer>().enabled = false;
		character.Find("M870").GetComponent<SkinnedMeshRenderer>().enabled = false;
		character.Find("AKM").GetComponent<SkinnedMeshRenderer>().enabled = false;

		CharacterJoint[] joints = rootRig.GetComponentsInChildren<CharacterJoint>();
		Rigidbody[] rigidbodies = rootRig.GetComponentsInChildren<Rigidbody>();
		Collider[] colliders = rootRig.GetComponentsInChildren<Collider>();
		
		foreach(CharacterJoint joint in joints) {
			joint.enablePreprocessing = false;
			joint.enableProjection = true;
		}
		foreach(Rigidbody rigidbody in rigidbodies) {
			rigidbody.useGravity = true;
			rigidbody.isKinematic = false;
		}
		foreach(Collider collider in colliders) {
			collider.enabled = true;
		}
		
	}

	void DeactivateRagdoll() {
		Rigidbody[] rigidbodies = rootRig.GetComponentsInChildren<Rigidbody>();
		Collider[] colliders = rootRig.GetComponentsInChildren<Collider>();

		foreach(Rigidbody rigidbody in rigidbodies) {
			rigidbody.useGravity = false;
			rigidbody.isKinematic = true;
		}
		foreach(Collider collider in colliders) {
			collider.enabled = false;
		}
	}

	void DisableWeapon(WeaponBase weapon) {
		weapon.IsEnabled = false;
	}

	void DisableController(FirstPersonController controller) {
		controller.enabled = false;
	}
	void OnControllerColliderHit(ControllerColliderHit hit) {
		if(hit.gameObject.tag == "BulletCase") {
			Physics.IgnoreCollision(GetComponent<Collider>(), hit.gameObject.GetComponent<Collider>());
		}
	}

	public void ActivateHealthRegeneration() {
		StartCoroutine(CoHealthRegeneration());
	}

	IEnumerator CoHealthRegeneration() {
		while(!healthManager.IsDead) {
			float nextDelay = 5f - (upgradeRegeneration * 0.35f);
			
			if(healthManager.Health < healthManager.MaxHealth) {
				healthManager.SetHealth(healthManager.Health + 1);
			}

			yield return new WaitForSeconds(nextDelay);
		}

		yield break;
	}
}
