using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager instance;

	private void Start()
	{
		if (instance != null)
		{
			Destroy(this);
		}
		else
		{
			instance = this;
			DontDestroyOnLoad(this);
		}

#if UNITY_STANDALONE
		Screen.SetResolution(576, 1024, false);
		Application.targetFrameRate = 60;
#endif
	}


}
