using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCharacter : MonoBehaviour {
	[Header("Managers")]
	public SoundManager soundManager;

	[Header("Weapon Sounds")]
	public AudioClip glockFire;
	public AudioClip glockMagOut;
	public AudioClip glockMagIn;
	public AudioClip glockBoltPulled;

	public AudioClip mp5KFire;
	public AudioClip mp5KMagOut;
	public AudioClip mp5KMagIn;
	public AudioClip mp5KBoltPulled;

	public AudioClip pythonFire;
	public AudioClip pythonMagOut;
	public AudioClip pythonMagIn;
	public AudioClip pythonBoltPulled;

	public AudioClip ump45Fire;
	public AudioClip ump45MagOut;
	public AudioClip ump45MagIn;
	public AudioClip ump45BoltPulled;

	public AudioClip m870Fire;
	
	public AudioClip akmFire;
	public AudioClip akmMagOut;
	public AudioClip akmMagIn;
	public AudioClip akmBoltPulled;

	[Header("Prefabs")]
	public GameObject gunSmoke;
	public GameObject emptyCase;
	
	NetworkPlayer networkPlayer;

	void Awake() {
		networkPlayer = transform.parent.GetComponent<NetworkPlayer>();
	}

	Transform GetMuzzlePoint() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				return transform.Find("Healthmale/Gun_Glock/MuzzlePoint");
			case Weapon.MP5K:
				return transform.Find("Healthmale/Gun_MP5K/MuzzlePoint");
			case Weapon.Python:
				return transform.Find("Healthmale/Gun_Python/MuzzlePoint");
			case Weapon.UMP45:
				return transform.Find("Healthmale/Gun_UMP45/MuzzlePoint");
			case Weapon.M870:
				return transform.Find("Healthmale/Gun_M870/MuzzlePoint");
			case Weapon.AKM:
				return transform.Find("Healthmale/Gun_AKM/MuzzlePoint");
		}

		return null;
	}

	Transform GetCaseSpawnPoint() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				return transform.Find("Healthmale/Gun_Glock/CaseSpawn");
			case Weapon.MP5K:
				return transform.Find("Healthmale/Gun_MP5K/CaseSpawn");
			case Weapon.Python:
				return null;
			case Weapon.UMP45:
				return transform.Find("Healthmale/Gun_UMP45/CaseSpawn");
			case Weapon.M870:
				return transform.Find("Healthmale/Gun_M870/CaseSpawn");
			case Weapon.AKM:
				return transform.Find("Healthmale/Gun_AKM/CaseSpawn");
		}

		return null;
	}

	ParticleSystem GetMuzzleflash() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				return transform.Find("Healthmale/Gun_Glock/Muzzleflash").GetComponent<ParticleSystem>();
			case Weapon.MP5K:
				return transform.Find("Healthmale/Gun_MP5K/Muzzleflash").GetComponent<ParticleSystem>();
			case Weapon.Python:
				return transform.Find("Healthmale/Gun_Python/Muzzleflash").GetComponent<ParticleSystem>();
			case Weapon.UMP45:
				return transform.Find("Healthmale/Gun_UMP45/Muzzleflash").GetComponent<ParticleSystem>();
			case Weapon.M870:
				return transform.Find("Healthmale/Gun_M870/Muzzleflash").GetComponent<ParticleSystem>();
			case Weapon.AKM:
				return transform.Find("Healthmale/Gun_AKM/Muzzleflash").GetComponent<ParticleSystem>();
		}

		return null;
	}

	void CreateGunSmoke() {
		Transform muzzlePoint = GetMuzzlePoint();
		GameObject gunSmokeEffect = Instantiate(gunSmoke, muzzlePoint.position, muzzlePoint.rotation);
		Destroy(gunSmokeEffect, 5f);
	}

	void CreateEjectingCase() {
		Transform caseSpawnPoint = GetCaseSpawnPoint();

		if(caseSpawnPoint == null) return;

		GameObject ejectedCase = Instantiate(emptyCase, caseSpawnPoint.position, caseSpawnPoint.rotation);
		Rigidbody caseRigidbody = ejectedCase.GetComponent<Rigidbody>();
		caseRigidbody.velocity = caseSpawnPoint.TransformDirection(-Vector3.left * 5.0f);
		caseRigidbody.AddTorque(Random.Range(-0.2f, 0.2f), Random.Range(0.1f, 0.2f), Random.Range(-0.2f, 0.2f));
		caseRigidbody.AddForce(0, Random.Range(2.0f, 4.0f), 0, ForceMode.Impulse);
		Destroy(ejectedCase, 10f);
	}

	void AnimateMuzzleflash() {
		ParticleSystem muzzleFlash = GetMuzzleflash();
		muzzleFlash.Play();
	}

	void OnFire() {
		CreateGunSmoke();
		CreateEjectingCase();
		AnimateMuzzleflash();
		PlayGunSound();		
	}

	void OnMagOut() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				soundManager.Play(glockMagOut);
				break;
			case Weapon.MP5K:
				soundManager.Play(mp5KMagOut);
				break;
			case Weapon.Python:
				soundManager.Play(pythonMagOut);
				break;
			case Weapon.UMP45:
				soundManager.Play(ump45MagOut);
				break;
			case Weapon.AKM:
				soundManager.Play(akmMagOut);
				break;
		}
	}

	void OnMagIn() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				soundManager.Play(glockMagIn);
				break;
			case Weapon.MP5K:
				soundManager.Play(mp5KMagIn);
				break;
			case Weapon.Python:
				soundManager.Play(pythonMagIn);
				break;
			case Weapon.UMP45:
				soundManager.Play(ump45MagIn);
				break;
			case Weapon.AKM:
				soundManager.Play(akmMagIn);
				break;
		}
	}

	void OnBoltPulled() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				soundManager.Play(glockBoltPulled);
				break;
			case Weapon.MP5K:
				soundManager.Play(mp5KBoltPulled);
				break;
			case Weapon.Python:
				soundManager.Play(pythonBoltPulled);
				break;
			case Weapon.UMP45:
				soundManager.Play(ump45BoltPulled);
				break;
			case Weapon.AKM:
				soundManager.Play(akmBoltPulled);
				break;
		}
	}

	void PlayGunSound() {
		switch(networkPlayer.currentWeapon) {
			case Weapon.Glock:
				soundManager.Play(glockFire);
				break;
			case Weapon.MP5K:
				soundManager.Play(mp5KFire);
				break;
			case Weapon.Python:
				soundManager.Play(pythonFire);
				break;
			case Weapon.UMP45:
				soundManager.Play(ump45Fire);
				break;
			case Weapon.M870:
				soundManager.Play(m870Fire);
				break;
			case Weapon.AKM:
				soundManager.Play(akmFire);
				break;
		}
	}
}
