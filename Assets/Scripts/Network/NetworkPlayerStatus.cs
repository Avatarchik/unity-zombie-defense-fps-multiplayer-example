using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;

public delegate void HttpCallback(string errorMessage);

public class NetworkPlayerStatus : MonoBehaviour {
	public string token;
	public string id;
	public int level;
	public int exp;
	public int rexp;
	public int totalPlays;
	public int totalKills;
	public int spentCash;
	public int killsGlock;
	public int killsMp5k;
	public int killsM870;
	public int killsAkm;
	public int killsPython;
	public int killsUmp45;

	public void Get(HttpCallback callback) {
		StartCoroutine(CoGetUserData(callback));
	}

	IEnumerator CoGetUserData(HttpCallback onComplete) {
		WWW httpResult = new WWW("http://YOUR_DOMAIN_HERE/user?token=" + token);

		yield return httpResult;

		if (httpResult.responseHeaders.Count > 0) {
			string statusText = httpResult.responseHeaders["STATUS"];
			int statusCode = HttpHelper.GetStatusCode(statusText);
			JsonData resultJson = JsonMapper.ToObject(httpResult.text);

			if(statusCode == 200) {
				level = (int) resultJson["level"];
				exp = (int) resultJson["exp"];
				rexp = (int) resultJson["rexp"];
				totalPlays = (int) resultJson["totalPlays"];
				totalKills = (int) resultJson["totalKills"];
				spentCash = (int) resultJson["spentCash"];
				killsGlock = (int) resultJson["kills"]["glock"];
				killsMp5k = (int) resultJson["kills"]["mp5k"];
				killsM870 = (int) resultJson["kills"]["m870"];
				killsAkm = (int) resultJson["kills"]["akm"];
				killsPython = (int) resultJson["kills"]["python"];
				killsUmp45 = (int) resultJson["kills"]["ump45"];

				onComplete(null);
			}
			else {
				string errorMessage = (string) resultJson["message"];
				onComplete(errorMessage);
			}
		}
		else {
			onComplete("Invalid HTTP Response");
		}
	}

	public void UpdateData(HttpCallback callback) {
		StartCoroutine(CoUpdateUserData(callback));
	}

	IEnumerator CoUpdateUserData(HttpCallback onComplete) {
		WWWForm formData = new WWWForm();
		formData.AddField("_method", "put");
		formData.AddField("level", level);
		formData.AddField("exp", exp);
		formData.AddField("rexp", rexp);
		formData.AddField("totalKills", totalKills);
		formData.AddField("spentCash", spentCash);
		formData.AddField("killsGlock", killsGlock);
		formData.AddField("killsMp5k", killsMp5k);
		formData.AddField("killsM870", killsM870);
		formData.AddField("killsAkm", killsAkm);
		formData.AddField("killsPython", killsPython);
		formData.AddField("killsUmp45", killsUmp45);

		WWW httpResult = new WWW("http://YOUR_DOMAIN_HERE/user?token=" + token, formData);

		yield return httpResult;

		if (httpResult.responseHeaders.Count > 0) {
			string statusText = httpResult.responseHeaders["STATUS"];
			int statusCode = HttpHelper.GetStatusCode(statusText);
			JsonData resultJson = JsonMapper.ToObject(httpResult.text);

			if(statusCode == 200) {
				onComplete(null);
			}
			else {
				string errorMessage = (string) resultJson["message"];
				onComplete(errorMessage);
			}
		}
		else {
			onComplete("Invalid HTTP Response");
		}
	}
}
