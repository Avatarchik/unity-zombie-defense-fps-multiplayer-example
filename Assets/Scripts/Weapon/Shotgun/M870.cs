using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class M870 : MonoBehaviour {
	[Header("Sounds")]
	public SoundManager soundManager;
	public AudioClip insertSound;
	public AudioClip pumpSound;

	void OnAmmoInsertion() {
		soundManager.Play(insertSound);
	}

	void OnPump() {
		soundManager.Play(pumpSound);
	}
}
