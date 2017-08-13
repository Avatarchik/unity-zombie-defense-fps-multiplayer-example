using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FundSystem : Photon.MonoBehaviour {
	[SerializeField] private int fund = 0;

	Text fundText;
	RewardText rewardText;

	void Start() {
		fundText = GameObject.Find("UI/InGameUI/PlayerUI/CharacterStatus/FundText").GetComponent<Text>();
		rewardText = GetComponent<RewardText>();
		UpdateUI();
	}

	void UpdateUI() {
		if(photonView.isMine && fundText != null) fundText.text = "Fund: " + fund.ToString() + " $";
	}

	public int GetFund() {
		return fund;
	}

	public void AddFund(int amount) {
		fund += amount;
		UpdateUI();

		photonView.RPC("RPCAddFund", PhotonTargets.Others, amount);
	}

	[PunRPC]
	void RPCAddFund(int amount) {
		fund += amount;

		UpdateUI();
	}

	public void AddBonus(int exp, int amount) {
		fund += amount;
		UpdateUI();

		photonView.RPC("RPCAddBonus", PhotonTargets.Others, exp, amount);
	}
	
	[PunRPC]
	void RPCAddBonus(int exp, int amount) {
		fund += amount;
		
		UpdateUI();
		rewardText.ShowBonus(exp, amount);
	}

	public void TakeFund(int amount) {
		fund -= amount;
		UpdateUI();

		photonView.RPC("RPCTakeFund", PhotonTargets.Others, amount);
	}

	[PunRPC]
	void RPCTakeFund(int amount) {
		fund -= amount;
	}
}
