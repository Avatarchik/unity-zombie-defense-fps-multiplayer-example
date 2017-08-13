using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteManager : MonoBehaviour {
	[Serializable]
	public struct SpriteItem {
		public string key;
		public Sprite sprite;
	}

	public SpriteItem[] sprites;

	public Sprite GetSprite(string key) {
		Sprite foundSprite = null;

		foreach(SpriteItem sprite in sprites) {
			if(sprite.key == key) {
				foundSprite = sprite.sprite;
				break;
			}
		}

		return foundSprite;
	}

	public static SpriteManager GetInstance() {
		return GameObject.Find("SpriteManager").GetComponent<SpriteManager>();
	}
}
