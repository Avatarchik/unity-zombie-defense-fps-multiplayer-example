using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager: MonoBehaviour {
	[Serializable]
	public struct PrefabItem {
		public string key;
		public GameObject prefab;
	}

	public PrefabItem[] prefabs;

	public GameObject GetPrefab(string key) {
		GameObject foundPrefab = null;

		foreach(PrefabItem prefabItem in prefabs) {
			if(prefabItem.key == key) {
				foundPrefab = prefabItem.prefab;
				break;
			}
		}

		return foundPrefab;
	}

	public static PrefabManager GetInstance() {
		return GameObject.Find("PrefabManager").GetComponent<PrefabManager>();
	}
}