using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
	public GameObject group_Login;
	public InputField inputF_Login_Id;
	public InputField inputF_Login_PW;

	public GameObject group_SignUp;
	public InputField inputF_SignUp_Id;
	public InputField inputF_SignUp_PW;

	public GameObject group_SetNickname;
	public InputField inputF_Nickname;

	public GameObject group_Loading;
	public Text text_LoadingMessage;

	public GameObject group_Error;
	public Text text_ErrorMessage;

	private void Start()
	{
		AddHandler();
		BackendManager.instance.AutoLogin();
	}

	private void OnDestroy()
	{
		RemoveHandler();
	}

	private void AddHandler()
	{
		BackendManager.instance.NeedNicknameEvent += GoToSetNickname;
		BackendManager.instance.NicknameExistEvent += LoadLobbyScene;
		BackendManager.instance.CreateNicknameEvent += OnCreateNickname;
		BackendManager.instance.LoadingEvent += Loading;
		BackendManager.instance.ErrorEvent += GotError;
	}

	private void RemoveHandler()
	{
		BackendManager.instance.NeedNicknameEvent -= GoToSetNickname;
		BackendManager.instance.NicknameExistEvent -= LoadLobbyScene;
		BackendManager.instance.CreateNicknameEvent -= OnCreateNickname;
		BackendManager.instance.LoadingEvent -= Loading;
		BackendManager.instance.ErrorEvent -= GotError;
	}

	public void Button_Login()
	{
		string id = inputF_Login_Id.text;
		string pW = inputF_Login_PW.text;

		BackendManager.instance.CustomLogin(id, pW);
	}

	public void Button_SignUp()
	{
		string id = inputF_SignUp_Id.text;
		string pW = inputF_SignUp_PW.text;

		BackendManager.instance.CustomSignUp(id, pW);
	}

	public void Button_SetNickname()
	{
		string nickname = inputF_Nickname.text;
		BackendManager.instance.CreateNickname(nickname);
	}

	public void Button_GoToSignUp()
	{
		group_Login.SetActive(false);
		group_SignUp.SetActive(true);
	}

	public void Button_BackToLogin()
	{
		group_Login.SetActive(true);
		group_SignUp.SetActive(false);
	}

	public void Button_CloseError()
	{
		group_Error.SetActive(false);
	}

	private void GoToSetNickname()
	{
		group_Login.SetActive(false);
		group_SignUp.SetActive(false);
		group_SetNickname.SetActive(true);
	}

	private void LoadLobbyScene()
	{
		Loading(true, ForWhat.none);
		SceneManager.LoadSceneAsync("Lobby");
	}

	private void OnCreateNickname(bool isSuccess)
	{
		if (isSuccess)
		{
			LoadLobbyScene();
			return;
		}
	}

	private void Loading(bool state, ForWhat what)
	{
		group_Loading.SetActive(state);
		string loadingMessage = "";
		switch (what)
		{
			case ForWhat.autoLogin:
			case ForWhat.login:
				loadingMessage = "로그인 중...";
				break;
			case ForWhat.createNickname:
				loadingMessage = "닉네임 생성 중...";
				break;
			default:
				break;
		}
		text_LoadingMessage.text = loadingMessage;
	}

	private void GotError(string msg, ForWhat what)
	{
		group_Error.SetActive(true);
		text_ErrorMessage.text = msg;
	}
}
