using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;

public class LoginForm : MonoBehaviour {
	public GameObject loginUI;
	public GameObject registerUI;
	public GameObject mainUI;
	public RegisterForm registerForm;
	InputField idText;
	InputField pwText;
	Button loginButton;
	Button registerButton;
	Button exitButton;
	bool isSubmit = false;

	void Start() {
		idText = transform.Find("IDText").GetComponent<InputField>();
		pwText = transform.Find("PwText").GetComponent<InputField>();
		loginButton = transform.Find("LoginButton").GetComponent<Button>();
		registerButton = transform.Find("JoinButton").GetComponent<Button>();
		exitButton = transform.parent.Find("ExitButton").GetComponent<Button>();

		HandleUIEvents();
	}

	void HandleUIEvents() {
		loginButton.onClick.AddListener(() => {
			if(isSubmit) {
				ShowFormError("It's already try to logging in...");
				return;
			}

			string id = idText.text;
			string pw = pwText.text;

			if(id == "") {
				ShowFormError("ID is required.");
				return;
			}
			else if(pw == "") {
				ShowFormError("Password is required.");
				return;
			}

			HideFormError();
			
			WWWForm formData = new WWWForm();
			formData.AddField("id", id);
			formData.AddField("password", pw);

			Submit(formData);
		});

		registerButton.onClick.AddListener(() => {
			HideFormError();
			registerForm.ResetForm();

			loginUI.SetActive(false);
			registerUI.SetActive(true);
		});

		exitButton.onClick.AddListener(() => {
			Application.Quit();
		});
	}

	public void ResetForm() {
		GameObject loginForm = GameObject.Find("UI/Lobby/LoginUI/LoginForm");
		loginForm.transform.Find("IDText").GetComponent<InputField>().text = "";
		loginForm.transform.Find("PwText").GetComponent<InputField>().text = "";
	}

	public void ShowFormError(string message) {
		GameObject errorText = loginUI.transform.Find("LoginForm/ErrorText").gameObject;
		errorText.GetComponent<Text>().text = message;
		errorText.SetActive(true);
	}
	
	public void HideFormError() {
		GameObject errorText = loginUI.transform.Find("LoginForm/ErrorText").gameObject;
		errorText.SetActive(false);
	}

	void Submit(WWWForm formData) {
		isSubmit = true;
		StartCoroutine(CoSubmit(formData));
	}

	IEnumerator CoSubmit(WWWForm formData) {
		WWW httpResult = new WWW("http://YOUR_DOMAIN_HERE/auth", formData);

		yield return httpResult;

		if (httpResult.responseHeaders.Count > 0) {
			string statusText = httpResult.responseHeaders["STATUS"];
			int statusCode = HttpHelper.GetStatusCode(statusText);
			JsonData resultJson = JsonMapper.ToObject(httpResult.text);

			if(statusCode == 200) {
				string token = (string) resultJson["token"];
				NetworkPlayerStatus playerStatus = GameObject.Find("GameManager").GetComponent<NetworkPlayerStatus>();

				playerStatus.token = token;
				playerStatus.id = idText.text;

				GameObject.Find("UI/Lobby/PlayerID").GetComponent<Text>().text = "PlayerID: " + idText.text;

				HideFormError();
				
				playerStatus.Get((string errorMessage) => {
					if(errorMessage != null) {
						ShowFormError(errorMessage);
						return;
					}

					GameObject.Find("UI/Lobby/PlayerID").GetComponent<Text>().text = "PlayerID: " + idText.text + " (Lv" + playerStatus.level + ")";

					mainUI.SetActive(true);
					loginUI.SetActive(false);
				});
			}
			else {
				string errorMessage = (string) resultJson["message"];
				ShowFormError(errorMessage);
			}
        }
		else {
			ShowFormError("Can't connect to Server.");
		}

		isSubmit = false;
	}
}
