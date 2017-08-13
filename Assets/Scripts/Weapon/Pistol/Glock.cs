using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glock : MonoBehaviour {
	[Header("Sounds")]
	public SoundManager soundManager;
	public AudioClip fireSound;
	public AudioClip magOutSound;
	public AudioClip magInSound;
	public AudioClip slideReleasedSound;
	public AudioClip slideDrawSound;

	void OnFire() {
		soundManager.Play(fireSound);
	}

	void OnDraw() {
		soundManager.Play(slideDrawSound);
	}

	void OnMagOut() {
		soundManager.Play(magOutSound);
	}

	void OnMagIn() {
		soundManager.Play(magInSound);
	}

	void OnBoltPulled() {
		soundManager.Play(slideReleasedSound);
	}
}
