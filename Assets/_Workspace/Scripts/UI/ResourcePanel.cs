﻿using DG.Tweening;
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

	private int lastResource = 0;
	// werewolf
	public GameObject werewolfPanel;
	public List<Image> eyeList;

	// hunter
	public GameObject hunterPanel;
	public List<Image> arrowList;

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
		Color color;

		switch (cType)
		{
			case ClassType.knight:
				image_Background.gameObject.SetActive(false);
				break;
			case ClassType.werewolf:
				ColorUtility.TryParseHtmlString("#890000", out color);
				image_Background.color = color;
				werewolfPanel.SetActive(true);
				break;
			case ClassType.hunter:
				ColorUtility.TryParseHtmlString("#06BD5D", out color);
				image_Background.color = color;
				hunterPanel.SetActive(true);
				break;
			default:
				image_Background.gameObject.SetActive(false);
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
				if (resource != lastResource)
				{
					for (int i = 0; i < resource; i++)
						eyeList[i].DOFade(1f, 1.3f);
				}
				lastResource = resource;
				break;
			case ClassType.hunter:
				resource /= 5;
				if (resource > lastResource)
				{
					for (int i = 0; i < resource; i++)
						arrowList[i].DOFade(1f, 0.5f);
				}
				else if (resource < lastResource)
				{
					for (int i = 0; i < arrowList.Count - resource; i++)
						arrowList[arrowList.Count - 1 - i].DOFade(0.4f, 0.5f);
				}
				lastResource = resource;
				break;
			default:
				break;
		}
	}
}
