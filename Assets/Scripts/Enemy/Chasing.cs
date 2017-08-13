using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Chasing : Photon.MonoBehaviour {
	NetworkEnemy networkEnemy;
	Animator animator;
	HealthManager healthManager;
	NavMeshAgent agent;

	[SerializeField] GameObject target;
	NetworkPlayer targetNetworkPlayer;
	public float damage = 15.0f;
	public bool isAttacking = false;
	public bool shouldChase = true;
	public bool isInLateUpdate = false;
	public bool shouldUpdate = true;

	IEnumerator distUpdateCo = null;

	bool isRetargeting = false;

	public GameObject Target {
		get {
			return target;
		}
	}

	// Use this for initialization
	void Awake () {
		animator = GetComponent<Animator>();
		healthManager = GetComponent<HealthManager>();
		networkEnemy = GetComponent<NetworkEnemy>();
		agent = GetComponent<NavMeshAgent>();

		if(!PhotonNetwork.isMasterClient) {
			agent.enabled = false;
		}
		else {
			StartCoroutine(CoRetargeting());
		}
	}

	// Update is called once per frame
	void Update () {
		if(!PhotonNetwork.isMasterClient) return;

		if(!target || !targetNetworkPlayer) {
			if(!isRetargeting && !healthManager.IsDead) {
				StartCoroutine(CoRetargeting());
			}
			
			return;
		}

		if(!healthManager.IsDead) {
			float distance = GetActualDistanceFromTarget();
			agent.destination = target.transform.position;	// remove this after resolved

			// Reduce calculation of path finding
			if(distance <= 20f) {
				if(distUpdateCo != null) {
					StopCoroutine(distUpdateCo);
				}

				isInLateUpdate = false;
				agent.destination = target.transform.position;
			}
			else if(!isInLateUpdate) {
				if(distance <= 40f) {
					distUpdateCo = LateDistanceUpdate(2f);
					StartCoroutine(distUpdateCo);
				}
				else if(distance <= 60) {
					distUpdateCo = LateDistanceUpdate(3f);
					StartCoroutine(distUpdateCo);
				}
				else if(distance <= 80) {
					distUpdateCo = LateDistanceUpdate(4f);
					StartCoroutine(distUpdateCo);
				}
				else {
					distUpdateCo = LateDistanceUpdate(5f);
					StartCoroutine(distUpdateCo);
				}
			}

			if(agent.pathPending) return;

			CheckAttack();
		}
	}

	IEnumerator CoRetargeting() {
		isRetargeting = true;

		SetTarget(GetClosestPlayer());

		yield return new WaitForSeconds(.5f);
		isRetargeting = false;
	}

	IEnumerator LateDistanceUpdate(float duration) {
		isInLateUpdate = true;
		agent.destination = target.transform.position;
		yield return new WaitForSeconds(duration);
		
		isInLateUpdate = false;
		distUpdateCo = null;

		StartCoroutine(CoRetargeting());
	}

	public void SetTarget(GameObject player) {
		target = player;

		if(player == null) {
			shouldUpdate = false;
			photonView.RPC("RPCSyncTarget", PhotonTargets.Others, "");
		}
		else {
			targetNetworkPlayer = target.GetComponent<NetworkPlayer>();
			photonView.RPC("RPCSyncTarget", PhotonTargets.Others, player.GetComponent<NetworkPlayer>().playerName);
		}
	}

	[PunRPC]
	void RPCSyncTarget(string playerName) {
		if(playerName == "") {
			shouldUpdate = false;
			return;
		}

		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

		foreach(GameObject player in players) {
			if(player.GetComponent<NetworkPlayer>().playerName == playerName) {
				target = player;
				targetNetworkPlayer = target.GetComponent<NetworkPlayer>();
				break;
			}
		}
	}

	GameObject GetClosestPlayer() {
		Vector3 position = transform.position;
		GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
		GameObject closestPlayer = null;
		float minimumDistance = 1000000f;

		foreach(GameObject player in players) {
			float distanceToPlayer = GetDistanceFrom(position, player.transform.position);

			if(distanceToPlayer < minimumDistance) {
				closestPlayer = player;
				minimumDistance = distanceToPlayer;
			}
		}

		return closestPlayer;
	}

	float GetActualDistanceFromTarget() {
		return GetDistanceFrom(target.transform.position, this.transform.position);
	}

	float GetDistanceFrom(Vector3 src, Vector3 dist) {
		return Vector3.Distance(src, dist);
	}

	public void StopChasing() {
		shouldUpdate = false;
		shouldChase = false;
		Destroy(agent);

		if(distUpdateCo != null) StopCoroutine(distUpdateCo);
		if(resetAttackCo != null) StopCoroutine(resetAttackCo);
	}

	float origSpeed;
	IEnumerator resetAttackCo = null;

	void CheckAttack() {
		// Calculate actual distance from target
		float distanceFromTarget = GetActualDistanceFromTarget();
		
		// Calculate direction is toward player
		Vector3 direction = target.transform.position - this.transform.position;
		float angle = Vector3.Angle(direction, this.transform.forward);

		if(!isAttacking && distanceFromTarget <= 2.0f && angle <= 60f) {
			isAttacking = true;
			shouldChase = false;

			origSpeed = agent.speed;
			agent.speed = 0;

			networkEnemy.TriggerAttackAnimation();

			HealthManager targetHealthManager = target.GetComponent<HealthManager>();

			if(targetHealthManager) {
				targetHealthManager.ApplyDamage(damage);
			}

			resetAttackCo = ResetAttacking();
			StartCoroutine(resetAttackCo);
		}
	}

	IEnumerator ResetAttacking() {
		yield return new WaitForSeconds(1.4f);

		isAttacking = false;
		shouldChase = true;

		if(!healthManager.IsDead) {
			agent.speed = origSpeed;
		}

		resetAttackCo = null;
		yield break;
	}

	public void SetDamage(float newDamage) {
		damage = newDamage;
		photonView.RPC("RPCSetDamage", PhotonTargets.Others, newDamage);
	}

	public void SetSpeed(float newSpeed, float newAngularSpeed) {
		agent.speed = newSpeed;
		agent.angularSpeed = newAngularSpeed;
		animator.SetFloat("SpeedMultiplier", newSpeed);
		
		photonView.RPC("RPCSetSpeed", PhotonTargets.Others, newSpeed, newAngularSpeed);
	}

	[PunRPC]
	void RPCSetDamage(float newDamage) {
		damage = newDamage;
	}

	[PunRPC]
	void RPCSetSpeed(float newSpeed, float newAngularSpeed) {
		agent.speed = newSpeed;
		agent.angularSpeed = newAngularSpeed;
		animator.SetFloat("SpeedMultiplier", newSpeed);
	}
}
