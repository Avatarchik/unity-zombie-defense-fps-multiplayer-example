using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCanvas : MonoBehaviour {
	Camera m_Camera;
	Text text;
	Slider hpSlider;
	NetworkPlayer networkPlayer;
	HealthManager healthManger;
	bool isLookingForCamera = false;

	void Start() {
		FindCamera();

		text = transform.Find("NameText").GetComponent<Text>();
		hpSlider = transform.Find("HPSlider").GetComponent<Slider>();
		networkPlayer = transform.parent.GetComponent<NetworkPlayer>();
		healthManger = transform.parent.GetComponent<HealthManager>();

		UpdateCanvas();
	}

	void FindCamera() {
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

		foreach(GameObject player in players) {
			NetworkPlayer networkPlayer = player.GetComponent<NetworkPlayer>();
			
			if(networkPlayer.IsLocalPlayer) {
				m_Camera = player.transform.Find("FirstPersonCharacter").GetComponent<Camera>();
				break;
			}
		}

		if(m_Camera == null) m_Camera = Camera.main;
	}

	void Update() {
		if(isLookingForCamera) return;

		if(!m_Camera) {
			StartCoroutine(CoFindCamera());
			return;
		}

		transform.LookAt(m_Camera.transform);
		transform.Rotate(0, 180, 0);

		UpdateCanvas();
	}

	IEnumerator CoFindCamera() {
		isLookingForCamera = true;

		FindCamera();

		yield return new WaitForSeconds(1f);
		isLookingForCamera = false;
	}

	void UpdateCanvas() {
		text.text = networkPlayer.playerName;

		if(Vector3.Distance(m_Camera.transform.position, transform.position) <= 20) {
			hpSlider.gameObject.SetActive(true);
			float percentage = (float) healthManger.Health / (float) 100;
			hpSlider.value = percentage;
		}
		else {
			hpSlider.gameObject.SetActive(false);
		}
	}
}
