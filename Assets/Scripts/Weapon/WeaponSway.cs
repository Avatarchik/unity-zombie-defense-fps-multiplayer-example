using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSway : MonoBehaviour {
	public float amount = 0.1f;
	public float maxAmount = 0.3f;
	public float smoothAmount = 6.0f;
	
	private Vector3 initPos;

	void Start() {
		initPos = transform.localPosition;
	}

	void Update() {
		float moveX = -Input.GetAxis("Mouse X") * amount;
		float moveY = -Input.GetAxis("Mouse Y") * amount;
		moveX = Mathf.Clamp(moveX, -maxAmount, maxAmount);
		moveY = Mathf.Clamp(moveY, -maxAmount, maxAmount);

		Vector3 finalPos = new Vector3(moveX, moveY, 0);
		transform.localPosition = Vector3.Lerp(transform.localPosition, finalPos + initPos, Time.deltaTime * smoothAmount);
	}
}