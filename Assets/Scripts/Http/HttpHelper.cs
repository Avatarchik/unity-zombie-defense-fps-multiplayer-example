using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HttpHelper : MonoBehaviour {
	public static int GetStatusCode(string statusText) {
		string[] statusTexts = statusText.Split(' ');
		string statusCode = statusTexts[1];

		return int.Parse(statusCode);
	}
}
