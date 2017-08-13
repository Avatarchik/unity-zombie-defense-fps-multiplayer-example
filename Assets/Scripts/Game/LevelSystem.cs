using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSystem : Photon.MonoBehaviour {
	public int level = 1;
	public int exp = 0;
	public int requireExp = 30;
	public Text levelText;
	public Text expText;
	public Slider expSlider;
	NetworkPlayerStatus playerStatus;
	Text playerIdText;

	void Start() {
		playerIdText = GameObject.Find("UI/Lobby/PlayerID").GetComponent<Text>();
		levelText = GameObject.Find("UI/InGameUI/PlayerUI/CharacterStatus/LevelText").GetComponent<Text>();
		expText = GameObject.Find("UI/InGameUI/PlayerUI/CharacterStatus/ExpText").GetComponent<Text>();
		expSlider = GameObject.Find("UI/InGameUI/PlayerUI/CharacterStatus/ExpText/Slider").GetComponent<Slider>();
		playerStatus = GameObject.Find("GameManager").GetComponent<NetworkPlayerStatus>();

		UpdateUI();
	}

	void UpdateUI() {
		if(photonView.isMine) {
			levelText.text = "Level: " + level;
			expText.text = "Exp: " + exp + " / " + requireExp;

			float percentage = (float) exp / (float) requireExp;
			expSlider.value = percentage;
		}
	}

	public int GetLevel() {
		return level;
	}

	public void GiveExp(int amount) {
		exp += amount;
		playerStatus.exp = exp;

		CheckLevelUp();
	}

	void CheckLevelUp() {
		if(exp >= requireExp) {
			exp = exp - requireExp;
			requireExp += 50;
			level++;

			playerStatus.exp = exp;
			playerStatus.rexp = requireExp;
			playerStatus.level = level;

			playerIdText.text = "PlayerID: " + playerStatus.id + " (Lv" + playerStatus.level + ")";

			CheckLevelUp();
		}

		UpdateUI();
	}
}
