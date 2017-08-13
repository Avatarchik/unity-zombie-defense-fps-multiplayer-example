using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;

public class RegisterForm : MonoBehaviour {
	public GameObject loginUI;
	public GameObject registerUI;
	public LoginForm loginForm;
	InputField idText;
	InputField pwText;
	InputField pwcText;
	Button submitButton;
	Button cancelButton;
	bool isSubmit = false;

	void Start() {
		idText = transform.Find("IDText").GetComponent<InputField>();
		pwText = transform.Find("PwText").GetComponent<InputField>();
		pwcText = transform.Find("PwcText").GetComponent<InputField>();
		submitButton = transform.Find("SubmitButton").GetComponent<Button>();
		cancelButton = transform.Find("CancelButton").GetComponent<Button>();

		HandleUIEvents();
	}

	void HandleUIEvents() {
		submitButton.onClick.AddListener(() => {
			if(isSubmit) {
				ShowFormError("It's already try to logging in...");
				return;
			}

			string id = idText.text;
			string pw = pwText.text;
			string pwc = pwcText.text;

			if(id == "") {
				ShowFormError("ID is required.");
				return;
			}
			else if(pw == "") {
				ShowFormError("Password is required.");
				return;
			}
			else if(pwc == "") {
				ShowFormError("Password confirm is required.");
				return;
			}
			else if(pw != pwc) {
				ShowFormError("Password doesn't match.");
				pwcText.text = "";
				return;
			}

			HideFormError();
			
			WWWForm formData = new WWWForm();
			formData.AddField("id", id);
			formData.AddField("password", pw);

			Submit(formData);
		});

		cancelButton.onClick.AddListener(() => {
			HideFormError();
			loginForm.ResetForm();

			registerUI.SetActive(false);
			loginUI.SetActive(true);
		});
	}

	public void ResetForm() {
		GameObject registerForm = GameObject.Find("UI/Lobby/RegisterUI/RegisterForm");
		registerForm.transform.Find("IDText").GetComponent<InputField>().text = "";
		registerForm.transform.Find("PwText").GetComponent<InputField>().text = "";
		registerForm.transform.Find("PwcText").GetComponent<InputField>().text = "";
	}

	public void ShowFormError(string message) {
		GameObject errorText = registerUI.transform.Find("RegisterForm/ErrorText").gameObject;
		errorText.GetComponent<Text>().text = message;
		errorText.SetActive(true);
	}
	
	public void HideFormError() {
		GameObject errorText = registerUI.transform.Find("RegisterForm/ErrorText").gameObject;
		errorText.SetActive(false);
	}

	void Submit(WWWForm formData) {
		isSubmit = true;
		StartCoroutine(CoSubmit(formData));
	}

	IEnumerator CoSubmit(WWWForm formData) {
		WWW httpResult = new WWW("http://YOUR_DOMAIN_HERE/user", formData);

		yield return httpResult;

		if (httpResult.responseHeaders.Count > 0) {
			string statusText = httpResult.responseHeaders["STATUS"];
			int statusCode = HttpHelper.GetStatusCode(statusText);

			if(statusCode == 200) {
				HideFormError();
				loginUI.transform.Find("LoginForm/IDText").GetComponent<InputField>().text = idText.text;

				loginUI.SetActive(true);
				registerUI.SetActive(false);
			}
			else {
				JsonData resultJson = JsonMapper.ToObject(httpResult.text);
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