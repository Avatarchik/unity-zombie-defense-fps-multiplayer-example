using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillReward : Photon.MonoBehaviour {
	public int exp;
	public int fund;

	void Start() {
		photonView.RPC("RPCSetReward", PhotonTargets.Others, exp, fund);
	}

	public void SetReward(int newExp, int newFund) {
		exp = newExp;
		fund = newFund;

		photonView.RPC("RPCSetReward", PhotonTargets.Others, exp, fund);
	}

	[PunRPC]
	void RPCSetReward(int newExp, int newFund) {
		exp = newExp;
		fund = newFund;
	}
}
