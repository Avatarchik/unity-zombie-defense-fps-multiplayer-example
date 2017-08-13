using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AllyIndicator : MonoBehaviour {
	RectTransform canvasRectTransform;
	RectTransform indicatorRectTransform;
	[SerializeField] GameObject target;
	bool initialized = false;

	void Start() {
		canvasRectTransform	= GameObject.Find("UI").GetComponent<RectTransform>();
		indicatorRectTransform = GetComponent<RectTransform>();
	}

	public void SetTarget(GameObject target) {
		this.target = target;
		initialized = true;
	}

	void Update() {
		if(target == null) {
			if(initialized) {
				Destroy(gameObject);
				return;	
			}
			// Wait until it's initialized
			else {
				return;
			}
		}

		Camera mainCam = Camera.main;

		if(mainCam) {
			Vector3 screenPoint = Camera.main.WorldToScreenPoint(target.transform.position);

			if(screenPoint.z < 0) {
				GetComponent<Image>().enabled = false;
			}
			else {
				GetComponent<Image>().enabled = true;
				indicatorRectTransform.anchoredPosition = (Vector2) screenPoint - canvasRectTransform.sizeDelta / 2f;
			}
		}
		else {
			GetComponent<Image>().enabled = false;
		}
	}
}