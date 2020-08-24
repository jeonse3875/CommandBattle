using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourcePanel : MonoBehaviour
{
	public ClassType cType;
	public Who who;
	public Image image_Background;
	private int resource = 0;
	private bool isActive = false;

	// werewolf
	public GameObject werewolfPanel;
	private int lastEye = 0;
	public List<Image> eyeList;

	private void Update()
	{
		if (isActive)
		{
			UpdatePanel();
		}
	}

	public void SetPanel(ClassType cType)
	{
		this.cType = cType;

		image_Background.gameObject.SetActive(true);

		switch (cType)
		{
			case ClassType.knight:
				image_Background.gameObject.SetActive(false);
				break;
			case ClassType.werewolf:
				ColorUtility.TryParseHtmlString("#890000", out Color color);
				image_Background.color = color;
				werewolfPanel.SetActive(true);
				break;
			default:
				break;
		}

		isActive = true;
	}

	public void UpdatePanel()
	{
		resource = InGame.instance.playerInfo[who].Resource;

		switch (cType)
		{
			case ClassType.knight:
				break;
			case ClassType.werewolf:
				resource = Mathf.Clamp(resource, 0, 3);
				if (resource != lastEye)
				{
					for (int i = 0; i < resource; i++)
						eyeList[i].DOFade(1f, 1.3f);
				}
				lastEye = resource;
				break;
			default:
				break;
		}
	}
}
