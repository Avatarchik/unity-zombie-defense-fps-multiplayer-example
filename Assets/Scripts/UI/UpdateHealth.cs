using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateHealth : MonoBehaviour {
	HealthManager healthManager;
	Text healthText;

	void Start() {
		healthManager = GetComponent<HealthManager>();
		healthText = GameObject.Find("UI/InGameUI/PlayerUI/CharacterStatus/HealthText").GetComponent<Text>();
	}

	void Update() {
		if(healthText) {
			healthText.text = "HP: " + healthManager.Health.ToString();
		}
	}	
}
