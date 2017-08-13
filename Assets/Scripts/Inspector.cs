using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof (CharacterController))]
public class Inspector: MonoBehaviour {
	float speed = 6.0f;
	Vector3 moveDirection = Vector3.zero;
	CharacterController controller;

	void Awake() {
		controller = GetComponent<CharacterController>();
	}

	void FixedUpdate() {
		moveDirection = new Vector3(
			Input.GetAxis("Horizontal"),
			Input.GetAxis("Jump") + (-Input.GetAxis("Crouch")),
			Input.GetAxis("Vertical")
		);

		moveDirection = transform.TransformDirection(moveDirection);
		moveDirection *= speed;

		controller.Move(moveDirection * Time.deltaTime);
	}
}
