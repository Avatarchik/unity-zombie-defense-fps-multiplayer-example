using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class NetworkPlayer : Photon.MonoBehaviour {
	public GameObject localCam;
    public Animator animator;

    WeaponManager weaponManager;
    public Weapon currentWeapon;
    FirstPersonController controller;
    HealthManager health;

    Dictionary<Weapon, GameObject> weapons = new Dictionary<Weapon, GameObject>();

	Vector3 syncPos = Vector3.zero;
    Quaternion syncRot = Quaternion.identity;
	Vector3 rSyncPos = Vector3.zero;
    Vector3 rSyncVelo = Vector3.zero;
    Quaternion rSyncRot = Quaternion.identity;
    Vector3 rSyncAngularVelo = Vector3.zero;
    Rigidbody rigidbody;
    bool isMoving = false;
    public string playerName;
    Vector3 oldPos;
    Quaternion oldRot;


    public bool IsLocalPlayer {
        get {
            return photonView.isMine;
        }
    }

	void Awake() {
		rigidbody = GetComponent<Rigidbody>();
        weaponManager = GetComponent<WeaponManager>();
        weaponManager.isLocalPlayer = photonView.isMine;

        health = GetComponent<HealthManager>();

        controller = GetComponent<FirstPersonController>();

        syncPos = transform.position;
        syncRot = transform.rotation;

        rSyncPos = rigidbody.position;
        rSyncRot = rigidbody.rotation;
        
        if(!photonView.isMine) {
            DisableScripts();

            weapons.Add(Weapon.Glock, transform.Find("Character/Glock").gameObject);
            weapons.Add(Weapon.Python, transform.Find("Character/Colt Python").gameObject);
            weapons.Add(Weapon.MP5K, transform.Find("Character/MP5K").gameObject);
            weapons.Add(Weapon.UMP45, transform.Find("Character/UMP45").gameObject);
            weapons.Add(Weapon.M870, transform.Find("Character/M870").gameObject);
            weapons.Add(Weapon.AKM, transform.Find("Character/AKM").gameObject);
        }
	}

    void Start () {
		if(photonView.isMine) {
            gameObject.transform.Find("Character").gameObject.SetActive(false);
            gameObject.transform.Find("PlayerCanvas").gameObject.SetActive(false);

            photonView.RPC("RPCSyncPlayerName", PhotonTargets.All, playerName);
		}
		else {
			localCam.SetActive(false);
            controller.enabled = false;
            gameObject.transform.Find("FirstPersonCharacter").gameObject.SetActive(false);
            gameObject.transform.Find("FirstPersonCharacter").GetComponent<Camera>().tag = "Untagged";  // There should be only one Main camera!
		}
	}

    [PunRPC]
    void RPCSyncPlayerName(string name) {
        playerName = name;
    }
    
	void Update() {
        weaponManager.isLocalPlayer = photonView.isMine;

        if(!photonView.isMine) {
            transform.position = Vector3.Lerp(transform.position, syncPos, 0.1f);
            transform.rotation = Quaternion.Lerp(transform.rotation, syncRot, 0.1f);

            UpdateWeapon();
            UpdateAnimator();
        }
        else {
            // Sync position only player moved
            if(oldPos != transform.position) {
                photonView.RPC("RPCSyncPlayerPosition", PhotonTargets.Others, 
                                transform.position, rigidbody.position, rigidbody.velocity, rigidbody.angularVelocity);
            }
            // Sync rotation only player moved
            if(oldRot != transform.rotation) {
                photonView.RPC("RPCSyncPlayerRotation", PhotonTargets.Others,
                                transform.rotation, rigidbody.rotation);
            }

            photonView.RPC("RPCSyncPlayerAnimValues", PhotonTargets.Others, weaponManager.currentWeapon, controller.IsMoving);

            oldPos = transform.position;
            oldRot = transform.rotation;
        }
    }
	public void FixedUpdate () {
        if(!photonView.isMine) {
            rigidbody.position = Vector3.Lerp(rigidbody.position, rSyncPos, 0.1f);
            rigidbody.rotation = Quaternion.Lerp(rigidbody.rotation, rSyncRot, 0.1f);
            rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, rSyncVelo, 0.1f);
            rigidbody.angularVelocity = Vector3.Lerp(rigidbody.angularVelocity, rSyncAngularVelo, 0.1f);
        }
    }

    void DisableScripts() {
        // LevelSystem levelSystem = GetComponentInChildren<LevelSystem>();
        // FundSystem fundSystem = GetComponentInChildren<FundSystem>();
        HealthManager healthManager = GetComponentInChildren<HealthManager>();
        UpdateHealth updateHealth = GetComponent<UpdateHealth>();
        WeaponBase[] weapons = GetComponentsInChildren<WeaponBase>();

        // levelSystem.enabled = false;
        // fundSystem.enabled = false;
        healthManager.enabled = false;
        updateHealth.enabled = false;

        foreach(WeaponBase weapon in weapons) {
            weapon.enabled = false;
        }
    }

	public void SwitchWeapon() {
        photonView.RPC("TriggerLocalWeaponSwitch", PhotonTargets.Others);
	}

    public void FireWeapon() {
        photonView.RPC("TriggerLocalWeaponFire", PhotonTargets.Others);
    }

    public void ReloadWeapon() {
        photonView.RPC("TriggerLocalWeaponReload", PhotonTargets.Others);
    }

    [PunRPC]
    void TriggerLocalWeaponSwitch() {
        animator.SetTrigger("Switch_Weapon");
    }

    [PunRPC]
    void TriggerLocalWeaponFire() {
        animator.SetTrigger("Fire");
    }

    [PunRPC]
    void TriggerLocalWeaponReload() {
        animator.SetTrigger("Reload");
    }

    [PunRPC]
    void TriggerCharacterMoving(bool set) {
        if(animator.gameObject.activeSelf) animator.SetBool("Walking", set);
    }

    [PunRPC]
    void RPCSyncPlayerPosition(Vector3 rPos, Vector3 rRPos, Vector3 rRVelo, Vector3 rRAvelo) {
        syncPos = rPos;
        rSyncPos = rRPos;
        rSyncVelo = rRVelo;
        rSyncAngularVelo = rRAvelo;
    }

    [PunRPC]
    void RPCSyncPlayerRotation(Quaternion rRot, Quaternion rRRot) {
        syncRot = rRot;
        rSyncRot = rRRot;
    }

    [PunRPC]
    void RPCSyncPlayerAnimValues(Weapon weapon, bool moving) {
        currentWeapon = weapon;
        isMoving = moving;
    }

    void UpdateWeapon() {
        Weapon[] weaponKeys = new Weapon[weapons.Keys.Count];
        weapons.Keys.CopyTo(weaponKeys, 0);

        for(int i = 0; i < weaponKeys.Length; i++) {
            if(weaponKeys[i] == currentWeapon) {
                weapons[weaponKeys[i]].SetActive(true);
            }
            else {
                weapons[weaponKeys[i]].SetActive(false);
            }
        }
    }

    void UpdateAnimator() {
		switch(currentWeapon) {
			case Weapon.Glock:
				animator.SetBool("Weapon_Glock", true);
				animator.SetBool("Weapon_MP5K", false);
                animator.SetBool("Weapon_Python", false);
                animator.SetBool("Weapon_UMP45", false);
				animator.SetBool("Weapon_M870", false);
				animator.SetBool("Weapon_AKM", false);
				break;
			case Weapon.MP5K:
				animator.SetBool("Weapon_Glock", false);
				animator.SetBool("Weapon_MP5K", true);
                animator.SetBool("Weapon_Python", false);
                animator.SetBool("Weapon_UMP45", false);
				animator.SetBool("Weapon_M870", false);
				animator.SetBool("Weapon_AKM", false);
				break;
            case Weapon.Python:
				animator.SetBool("Weapon_Glock", false);
				animator.SetBool("Weapon_MP5K", false);
                animator.SetBool("Weapon_Python", true);
                animator.SetBool("Weapon_UMP45", false);
				animator.SetBool("Weapon_M870", false);
				animator.SetBool("Weapon_AKM", false);
				break;
            case Weapon.UMP45:
				animator.SetBool("Weapon_Glock", false);
				animator.SetBool("Weapon_MP5K", false);
                animator.SetBool("Weapon_Python", false);
                animator.SetBool("Weapon_UMP45", true);
				animator.SetBool("Weapon_M870", false);
				animator.SetBool("Weapon_AKM", false);
				break;
			case Weapon.M870:
				animator.SetBool("Weapon_Glock", false);
				animator.SetBool("Weapon_MP5K", false);
                animator.SetBool("Weapon_Python", false);
                animator.SetBool("Weapon_UMP45", false);
				animator.SetBool("Weapon_M870", true);
				animator.SetBool("Weapon_AKM", false);
				break;
			case Weapon.AKM:
				animator.SetBool("Weapon_Glock", false);
				animator.SetBool("Weapon_MP5K", false);
                animator.SetBool("Weapon_Python", false);
                animator.SetBool("Weapon_UMP45", false);
				animator.SetBool("Weapon_M870", false);
				animator.SetBool("Weapon_AKM", true);
				break;
		}

        if(animator && !health.IsDead) {
            animator.SetBool("Walking", isMoving);
            photonView.RPC("TriggerCharacterMoving", PhotonTargets.Others, isMoving);
        }
	}
}
