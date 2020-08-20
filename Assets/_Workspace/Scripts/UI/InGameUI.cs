using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
	public CommandSet playerCommandSet;

	public GameObject group_DirUI;

	public GameObject group_BattleTap;

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
	public Slider slider_HP_P2;
	public Text text_HP_P1;
	public Text text_HP_P2;

	public GameObject youArrow;

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
		slider_HP_P1.value = 1f;
		slider_HP_P2.value = 1f;

		Instantiate(youArrow, InGame.instance.playerInfo[InGame.instance.me].tr.position, Quaternion.identity);
	}

	public void InitializeVariable()
	{
		playerCommandSet = new CommandSet();
		UpdateCapacity();
		InitializeCommandButton();
		UpdateCommandButton();
		isCompleted = false;
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

	public void Button_CompleteCommand()
	{
		isCompleted = true;

		while (playerCommandSet.GetTotalTime() < 10)
		{
			playerCommandSet.Push(new EmptyCommand());
		}

		CommandCompleteMsg ccm = playerCommandSet.ToCCM();
		BackendManager.instance.SendData(ccm);

		SetInteractability(false);
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

	public void Button_SetDirection(int dirNum)
	{
		Direction dir = (Direction)dirNum;
		currentCommand.dir = dir;
		playerCommandSet.Push(currentCommand);
		group_DirUI.SetActive(false);
		UpdateCapacity();
		UpdateCommandButton();
	}

	private void UpdateCapacity()
	{
		int total = playerCommandSet.GetTotalTime();
		setCapicity.text = total.ToString() + " / 10";
		capacitySlider.value = total / 10f;
	}

	public void UpdateHealth(Who who)
	{
		int max = InGame.instance.playerInfo[who].maxHP;
		int cur = InGame.instance.playerInfo[who].hp;
		if (who.Equals(Who.p1))
		{
			slider_HP_P1.value = cur / (float)max;
			text_HP_P1.text = string.Format("{0} / {1}", cur.ToString(), max.ToString());
		}
		else
		{
			slider_HP_P2.value = cur / (float)max;
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
		foreach(var button in commandButtons)
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

			if (leftLimit.Equals(0) || button.command.time > leftTime)
				button.button.interactable = false;
			else
				button.button.interactable = true;
		}
	}

	public void SetInteractability(bool status)
	{
		group_BattleTap.SetActive(!status);
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
}
