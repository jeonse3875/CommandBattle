using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
	public Image image_Blind;

	public GameObject group_ChangePhase;
	public Image image_LightBackground;
	public Image image_Light;

	public CommandSet playerCommandSet;

	public GameObject group_Introduce;
	public Text text_Nickname;
	public Image image_ClassIcon;
	public Text text_ClassName;

	public GameObject group_CommandUI;

	public GameObject group_DirUI;

	public GameObject group_WaitOpponent;

	public GameObject group_BattleTap;

	public GameObject group_MatchResult;
	public Text text_WinOrLose;

	public Command currentCommand;

	private bool isTimerActive = false;
	private float leftTime;
	private bool isCompleted = false;
	public Text text_Timer;

	public Slider capacitySlider;
	public Text setCapicity;

	public List<CommandButton> commandButtons;
	public List<Button> allDir;
	public List<Button> crossDir;
	public List<Button> diaDir;
	public Image image_DirCommand;

	public Text text_Nickname_P1;
	public Text text_Nickname_P2;
	public Slider slider_HP_P1;
	public Image image_HPBar_P1;
	public Slider slider_HP_P2;
	public Image image_HPBar_P2;
	public Text text_HP_P1;
	public Text text_HP_P2;

	public GameObject logBlock;
	public Transform logParentTr_P1;
	public Transform logParentTr_P2;
	public ResourcePanel resourcePanel_P1;
	public ResourcePanel resourcePanel_P2;
	private List<GameObject> logList = new List<GameObject>();

	public GameObject button_ViewLastBattle;
	public GameObject button_BackToCommandUI;
	public Text text_LeaveEnd;

	public List<Image> pushedCommandList;

	private int curResource = 0;

	private void Update()
	{
		if (isTimerActive)
		{
			leftTime -= Time.deltaTime;
			text_Timer.text = Mathf.CeilToInt(leftTime).ToString();
			if (leftTime < 0f)
			{
				isTimerActive = false;
				if (!isCompleted)
					Button_CompleteCommand();
			}
		}
	}

	public float DoFadeOut()
	{
		float fadeTime = 1f;
		image_Blind.gameObject.SetActive(true);
		image_Blind.DOFade(0f, fadeTime).OnComplete(() =>
		{
			image_Blind.gameObject.SetActive(false);
		});

		return fadeTime;
	}

	public void StartMakingCommand()
	{
		SetActiveCommandUI();
		Invoke("StartTimer", 2f);
		LightEffect();
	}

	public void LightEffect()
	{
		group_ChangePhase.SetActive(true);
		DOTween.Sequence()
			.Append(image_Light.transform.DOScale(1f, 0.5f).SetEase(Ease.OutCirc))
			.Join(image_LightBackground.DOFade(0.2f, 0.5f).SetEase(Ease.OutCirc))
			.Insert(2f, image_Light.transform.DOScale(0.1f, 0.15f))
			.Join(image_LightBackground.DOFade(0f, 0.15f))
			.OnComplete(() => { group_ChangePhase.SetActive(false); });
	}

	public void InitializeGame()
	{
		if (BackendManager.instance.isP1)
		{
			text_Nickname_P1.text = InGame.instance.playerNickname;
			text_Nickname_P2.text = InGame.instance.opponentNickname;
		}
		else
		{
			text_Nickname_P2.text = InGame.instance.playerNickname;
			text_Nickname_P1.text = InGame.instance.opponentNickname;
		}

		UpdateHealth(Who.p1);
		UpdateHealth(Who.p2);

		resourcePanel_P1.SetPanel(InGame.instance.playingCType[Who.p1]);
		resourcePanel_P2.SetPanel(InGame.instance.playingCType[Who.p2]);

		button_ViewLastBattle.SetActive(false);
		button_BackToCommandUI.SetActive(false);

		IntroducePlayer(Who.none);
	}

	public void IntroducePlayer(Who who)
	{
		group_Introduce.SetActive(true);

		if(who.Equals(Who.none))
		{
			image_ClassIcon.gameObject.SetActive(false);
			text_ClassName.text = "";
			text_Nickname.text = "";
			return;
		}

		image_ClassIcon.gameObject.SetActive(true);

		if (InGame.instance.me.Equals(who))
			text_Nickname.text = InGame.instance.playerNickname;
		else
			text_Nickname.text = InGame.instance.opponentNickname;
				
		text_ClassName.text = Command.GetKoreanClassName(InGame.instance.playingCType[who]);
		image_ClassIcon.sprite = Command.GetClassIcon(InGame.instance.playingCType[who]);
	}

	public void InitializeVariable()
	{
		group_Introduce.SetActive(false);
		playerCommandSet = new CommandSet();
		curResource = InGame.instance.playerInfo[InGame.instance.me].Resource;
		UpdateCapacity();
		InitializeCommandButton();
		UpdateCommandButton();
		isCompleted = false;

		button_ViewLastBattle.SetActive(InGame.instance.canViewLastBattle);
		button_BackToCommandUI.SetActive(true);
	}

	public void StartBattle()
	{
		ClearBattleLog();
		StopTimer();
		SetActiveBattleTapUI();
		LightEffect();
	}

	public void Button_AddCommand(int num)
	{
		currentCommand = Command.FromId(UserInfo.instance.mountedCommands[UserInfo.instance.playingClass][num]);
		if (currentCommand.dirType.Equals(DirectionType.none))
		{
			playerCommandSet.Push(currentCommand);
		}
		else
		{
			SetDirUI(currentCommand.dirType);
		}
		UpdateCapacity();
		UpdateCommandButton();
	}

	public void Button_ViewLastBattle(bool status)
	{
		group_BattleTap.SetActive(status);
	}

	public void Button_CompleteCommand()
	{
		isCompleted = true;

		while (playerCommandSet.GetTotalTime() < 10)
		{
			playerCommandSet.Push(new EmptyCommand());
		}

		CommandCompleteMsg ccm = playerCommandSet.ToCCM();
		BackendManager.instance.SendData(ccm);

		SetActiveWaitingUI();
	}

	public void Button_CancelDirUI()
	{
		group_DirUI.SetActive(false);
	}

	public void Button_ClearCommandSet()
	{
		playerCommandSet.Clear();
		UpdateCapacity();
		UpdateCommandButton();
	}

	public void Button_RemoveLastCommand()
	{
		playerCommandSet.Pop();
		UpdateCapacity();
		UpdateCommandButton();
	}

	public void Button_SetDirection(int dirNum)
	{
		Direction dir = (Direction)dirNum;
		currentCommand.dir = dir;
		playerCommandSet.Push(currentCommand);
		group_DirUI.SetActive(false);
		UpdateCapacity();
		UpdateCommandButton();
	}

	public void Button_ExitGame()
	{
		Debug.Log("게임을 종료하고 로비로 나갑니다.");
		SceneManager.LoadScene("Lobby");
	}

	private void UpdateCapacity()
	{
		int total = playerCommandSet.GetTotalTime();
		string color;

		if (total <= 2)
			color = "#F13242";
		else if (total <= 9)
			color = "#FF8400";
		else
			color = "#03BD5B";

		setCapicity.text = string.Format("<color={1}>{0}</color>/10", total, color);
		if (total.Equals(0))
			capacitySlider.value = 0f;
		else
			capacitySlider.value = Mathf.Clamp(total / 10f + 0.01f, 0f, 1f);

		int index = 0;

		foreach(var image in pushedCommandList)
		{
			image.gameObject.SetActive(false);
		}

		foreach(var command in playerCommandSet.commandList)
		{
			var image = pushedCommandList[index];
			image.sprite = command.GetCommandIcon();
			image.gameObject.SetActive(true);
			index += command.time;
		}
	}

	public void UpdateHealth(Who who)
	{
		int max = InGame.instance.playerInfo[who].maxHP;
		int cur = InGame.instance.playerInfo[who].HP;
		float value = cur / (float)max;

		Color color;
		if (value > 0.5f)
			ColorUtility.TryParseHtmlString("#06BD5D", out color);
		else if (value > 0.25f)
			ColorUtility.TryParseHtmlString("#FFB700", out color);
		else
			ColorUtility.TryParseHtmlString("#F13242", out color);

		if (who.Equals(Who.p1))
		{
			slider_HP_P1.value = value;
			image_HPBar_P1.color = color;
			text_HP_P1.text = string.Format("{0} / {1}", cur.ToString(), max.ToString());
		}
		else
		{
			slider_HP_P2.value = value;
			image_HPBar_P2.color = color;
			text_HP_P2.text = string.Format("{0} / {1}", cur.ToString(), max.ToString());
		}
	}

	private void SetDirUI(DirectionType dirType)
	{
		image_DirCommand.sprite = currentCommand.GetCommandIcon();

		foreach (var button in allDir)
			button.gameObject.SetActive(false);

		switch (dirType)
		{
			case DirectionType.cross:
				foreach (var button in crossDir)
					button.gameObject.SetActive(true);
				break;
			case DirectionType.diagonal:
				foreach (var button in diaDir)
					button.gameObject.SetActive(true);
				break;
			case DirectionType.all:
				foreach (var button in allDir)
					button.gameObject.SetActive(true);
				break;
		}
		group_DirUI.SetActive(true);
	}

	private void InitializeCommandButton()
	{
		int commandCount = UserInfo.instance.mountedCommands[UserInfo.instance.playingClass].Count;
		int index = 0;
		foreach (var button in commandButtons)
		{
			if (index < commandCount)
			{
				CommandId id = UserInfo.instance.mountedCommands[UserInfo.instance.playingClass][index];
				button.InitializeButton(id);
			}
			else
			{
				button.SetUnuseButton();
			}
			index++;
		}
	}

	private void UpdateCommandButton()
	{
		foreach(CommandButton button in commandButtons)
		{
			if (!button.gameObject.activeSelf)
				break;

			int count;
			int leftLimit;

			if (button.command.limit.Equals(10))
			{
				leftLimit = 1;
			}
			else
			{
				count = playerCommandSet.GetCommandCount(button.command.id);
				leftLimit = button.command.limit - count;
				button.text_Left.text = "X " + leftLimit.ToString();
			}

			int leftTime = 10 - playerCommandSet.GetTotalTime();

			bool isLimitLeft = !leftLimit.Equals(0);
			bool isTimeLeft = button.command.time <= leftTime;
			bool isCostLeft = button.command.costResource <= (curResource - playerCommandSet.GetTotalCost());

			bool canUse = isLimitLeft && isTimeLeft && isCostLeft;

			canUse = canUse && button.command.CanUse();
			button.button.interactable = canUse;
		}
	}

	public void SetActiveCommandUI()
	{
		group_CommandUI.SetActive(true);
		group_WaitOpponent.SetActive(false);
		group_BattleTap.SetActive(false);
	}

	public void SetActiveWaitingUI()
	{
		group_WaitOpponent.SetActive(true);
		group_BattleTap.SetActive(false);
	}

	public void SetActiveBattleTapUI()
	{
		group_CommandUI.SetActive(false);
		group_WaitOpponent.SetActive(false);
		group_BattleTap.SetActive(true);
	}

	public void StartTimer()
	{
		leftTime = 60f;
		isTimerActive = true;
		text_Timer.gameObject.SetActive(true);
	}

	public void StopTimer()
	{
		isTimerActive = false;
		text_Timer.gameObject.SetActive(false);
	}

	public void InstantiateBattleLog(Command command, string message)
	{
		GameObject obj; 
		if (command.commander.Equals(Who.p1))
			obj = Instantiate(logBlock, logParentTr_P1);
		else
			obj = Instantiate(logBlock, logParentTr_P2);

		obj.transform.SetAsFirstSibling();
		logList.Add(obj);
		obj.GetComponent<LogBlock>().SetBlock(command, message);
	}

	public void ClearBattleLog()
	{
		button_BackToCommandUI.SetActive(false);
		foreach(var log in logList)
			Destroy(log);
		logList.Clear();
	}

	public void SetMatchResultUI(bool win, bool isDraw = false, bool isLeaveEnd = false)
	{
		group_MatchResult.SetActive(true);

		if (isDraw)
		{
			text_WinOrLose.text = "무승부";
			group_ChangePhase.SetActive(true);
			image_LightBackground.DOFade(0.5f, 3f);
			return;
		}

		if(isLeaveEnd)
		{
			text_LeaveEnd.gameObject.SetActive(true);
		}

		if (win)
		{
			text_WinOrLose.text = "승리";
			group_ChangePhase.SetActive(true);
			image_LightBackground.gameObject.SetActive(false);
			image_Light.transform.DOScale(1f, 0.5f).SetEase(Ease.OutCirc);
		}
		else
		{
			text_WinOrLose.text = "패배";
			group_ChangePhase.SetActive(true);
			image_LightBackground.DOFade(0.5f, 3f);
		}
	}
}
