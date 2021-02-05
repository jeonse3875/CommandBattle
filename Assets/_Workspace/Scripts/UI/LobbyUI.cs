using BackEnd.Tcp;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
	public Image image_Blind;

	public GameObject group_Loading;
	public Text text_LoadingMessage;
	public GameObject button_CancelMatchmaking;

	public GameObject group_Error;
	public Text text_ErrorMessage;

	public GameObject group_Main;

	public GameObject group_Item;

	public GameObject group_CommandList;
	public GameObject arrow_CommandList;

	public GameObject group_MountedCommand;
	public GameObject arrow_MountedList;

	public GameObject group_MountCommon;

	public GameObject group_PlayingClass;

	public GameObject group_ExitGame;

	public bool isInit = true;


	public Transform commandListTapParentTr;
	public Transform mountedCommandTapParentTr;
	public GameObject tap;
	public Transform commandListScrollParentTr;
	public GameObject commandListScroll;
	public GameObject passiveBlock;
	public GameObject commandInfoBlock;
	public Transform mountedListScrollParentTr;
	public GameObject mountedListScroll;
	public Transform classBlockParentTr_MountCommon;
	public Transform classBlockParentTr_PlayingClass;
	public GameObject classBlock;
	public Dictionary<ClassType, ClassTap> classTapDic_List = new Dictionary<ClassType, ClassTap>();
	public Dictionary<ClassType, ClassTap> classTapDic_Mounted = new Dictionary<ClassType, ClassTap>();
	public Dictionary<ClassType, GameObject> commandListObjDic = new Dictionary<ClassType, GameObject>();
	public Dictionary<ClassType, GameObject> mountedListObjDic = new Dictionary<ClassType, GameObject>();

	public int tapMode = 0; // 0 = list, 1 = mounted

	public CommandId commonCommandIdForWaiting;

	public GameObject group_Detail;
	public GameObject previewCamera;
	public Text text_DetailCommandName;
	public TextMeshProUGUI text_DetailDescription;
	public Text text_DetailTime;
	public Text text_DetailLimit;
	public Text text_DetailDamage;

	public GameObject group_PassiveDetail;
	public GameObject characterPreviewCamera;
	private GameObject characterPreviewObj;
	public Image image_PDetail_ClassIcon;
	public Text text_PDetailClassName;
	public Text text_PDetailHP;
	public Text text_PassiveName;
	public TextMeshProUGUI text_PassiveDescription;
	public Text text_BattlePoint;

	public GameObject pre_AttackRange;
	public Grid pre_Grid;
	public PlayerInfo pre_Player;
	public PlayerInfo pre_Enemy;
	public Transform pre_Particles;
	public GameObject damageText;

	private Coroutine previewRoutine;
	private Coroutine previewExeRoutine;
	private Command pre_Command;
	private List<GameObject> pre_ObjList = new List<GameObject>();
	private List<DamageTMP> pre_TMPList = new List<DamageTMP>();
	public BuffSet pre_BuffSet1;
	public BuffSet pre_BuffSet2;
	private GameObject pre_P1Obj;
	private GameObject pre_P2Obj;

	private string randomOneOnOneIndate = "2020-07-07T07:29:09.015Z";

	#region LifeCycle

	private void Start()
	{
		if (!UserInfo.instance.isUpdatedCommandData)
			UserInfo.instance.UpdateCommandInfo();
		if (!UserInfo.instance.isUpdatedRecordData)
			UserInfo.instance.UpdateRecordInfo();
		if (!UserInfo.instance.isUpdatedMoneyData)
			UserInfo.instance.UpdateMoneyInfo();
		UserInfo.instance.GiveAllCommand(); // Test
		BackendManager.instance.JoinMatchingServer();
		AddHandler();
		SetPreview();
		InitializeCommandInfoUI();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Escape))
			Button_GoToExitGame();
		text_BattlePoint.text = UserInfo.instance.battlePoint.ToString();
	}

	private void LateUpdate()
	{
		if (pre_Player != null && pre_Enemy != null)
		{
			pre_Player.SetPos(pre_Grid.Vec3ToPos(pre_Player.tr.position));
			pre_Enemy.SetPos(pre_Grid.Vec3ToPos(pre_Enemy.tr.position));
		}
		if (pre_BuffSet1 != null && pre_BuffSet2 != null)
		{
			pre_BuffSet1.Update(Time.deltaTime);
			pre_BuffSet2.Update(Time.deltaTime);
		}
	}

	private void OnDestroy()
	{
		RemoveHandler();
	}

	private void AddHandler()
	{
		BackendManager.instance.LoadingEvent += Loading;
		BackendManager.instance.ErrorEvent += GetError;
		BackendManager.instance.GameStartEvent += GoToInGameScene;
		UserInfo.instance.UserInfoErrorEvent += GetError;
	}

	private void RemoveHandler()
	{
		BackendManager.instance.LoadingEvent -= Loading;
		BackendManager.instance.ErrorEvent -= GetError;
		BackendManager.instance.GameStartEvent -= GoToInGameScene;
		UserInfo.instance.UserInfoErrorEvent -= GetError;
	}

	#endregion

	#region Preview

	private void SetPreview()
	{
		pre_Grid = new Grid(5, 5, 5f, new Vector3(-10, 0, -10));
		pre_Particles = GameObject.Find("Particles_p1").transform;
	}

	public void StartPreview(Command command)
	{
		ClassType cType = command.classType;
		if (command.classType.Equals(ClassType.common))
			cType = ClassType.knight;

		if (pre_P1Obj != null && pre_P2Obj != null)
		{
			Destroy(pre_P1Obj);
			Destroy(pre_P2Obj);
		}

		pre_P1Obj = Instantiate(Resources.Load<GameObject>("Character/" + cType.ToString() + "_Blue"));
		pre_P2Obj = Instantiate(Resources.Load<GameObject>("Character/" + cType.ToString() + "_Red"));

		pre_Player = new PlayerInfo(Who.p1, (1, 2), pre_P1Obj.transform)
		{
			isPreview = true,
			lobby = this
		};
		pre_Enemy = new PlayerInfo(Who.p2, (3, 2), pre_P2Obj.transform)
		{
			isPreview = true,
			lobby = this
		};

		pre_BuffSet1 = new BuffSet(pre_Player);
		pre_BuffSet2 = new BuffSet(pre_Enemy);

		command.SetPreview(pre_Grid, pre_Player, pre_Enemy, pre_Particles, (pre_BuffSet1, pre_BuffSet2));
		pre_Command = command;
		pre_Command.commander = Who.p1;
		previewRoutine = StartCoroutine(Preview());
	}

	private IEnumerator Preview()
	{
		WaitForSeconds wait0 = new WaitForSeconds(0.5f);
		WaitForSeconds wait1 = new WaitForSeconds(pre_Command.time);
		WaitForSeconds wait2 = new WaitForSeconds(0.8f);
		pre_Player.transformCount = 1;

		while (true)
		{
			if (pre_Player.transformCount.Equals(0))
			{
				pre_Player.transformCount = 1;
				pre_Player.specialize.Transform();
			}
			else
			{
				pre_Player.transformCount = 0;
				pre_Player.specialize.DeTransform();
			}
			pre_Player.SetPos(pre_Command.previewPos.player);
			pre_Player.tr.position = pre_Grid.PosToVec3(pre_Command.previewPos.player);
			pre_Enemy.SetPos(pre_Command.previewPos.enemy);
			pre_Enemy.tr.position = pre_Grid.PosToVec3(pre_Command.previewPos.enemy);
			pre_Player.LookEnemy();
			pre_Enemy.tr.LookAt(pre_Player.tr);

			yield return wait0;
			previewExeRoutine = StartCoroutine(pre_Command.Execute());
			yield return wait1;
			pre_Command.SetAnimState(AnimState.idle);
			yield return wait2;

			ResetPreview();
		}
	}

	private void ResetPreview()
	{
		pre_BuffSet1.Clear();
		pre_BuffSet2.Clear();

		pre_Player.SetAnimState(AnimState.idle);
		pre_Enemy.SetAnimState(AnimState.idle);

		foreach (var tmp in pre_TMPList)
		{
			if (tmp != null)
				tmp.seq.Kill();
		}

		foreach (var obj in pre_ObjList)
		{
			if (obj != null)
				Destroy(obj);
		}
		pre_TMPList.Clear();
		pre_ObjList.Clear();
	}

	public GameObject InstantiateTrap(CommandId id)
	{
		GameObject obj = Instantiate(Resources.Load<GameObject>(string.Format("Trap/{0}", id.ToString())));
		pre_ObjList.Add(obj);
		return obj;
	}

	public void SetCharacterPreview(ClassType cType)
	{
		characterPreviewObj = Instantiate(Resources.Load<GameObject>(string.Format("Character/{0}_Blue", cType.ToString())));
		characterPreviewObj.transform.position = new Vector3(-100f, 0, 0);
		characterPreviewObj.GetComponent<ClassSpecialize>().Initialize();
		characterPreviewCamera.SetActive(true);
	}
	#endregion

	#region Button

	public void Button_StartRandomMatchMaking()
	{
		UserInfo.instance.playingGameMode = GameMode.OneOnOne;
		BackendManager.instance.JoinMatchingServer();
		group_PlayingClass.SetActive(true);
	}

	public void Button_CancelMatchmaking()
	{
		BackendManager.instance.CancelMatchMaking();
	}

	public void Button_GoToBossRush()
	{
		UserInfo.instance.playingGameMode = GameMode.bossRush;
		group_PlayingClass.SetActive(true);
	}

	public void Button_GoToItem()
	{
		group_Main.SetActive(false);
		group_Item.SetActive(true);
	}

	public void Button_BackToMain()
	{
		group_Main.SetActive(true);
		group_Item.SetActive(false);

		if (UserInfo.instance.isUpdatedCommandData)
			UserInfo.instance.UploadCommandInfo();
	}

	public void Button_CommandList()
	{
		tapMode = 0;
		group_CommandList.SetActive(true);
		group_MountedCommand.SetActive(false);
		arrow_CommandList.SetActive(true);
		arrow_MountedList.SetActive(false);
	}

	public void Button_MountedCommand()
	{
		tapMode = 1;
		group_MountedCommand.SetActive(true);
		group_CommandList.SetActive(false);
		arrow_CommandList.SetActive(false);
		arrow_MountedList.SetActive(true);
	}

	public void Button_CloseMountCommon()
	{
		group_MountCommon.SetActive(false);
	}

	public void Button_ClosePlayingClass()
	{
		group_PlayingClass.SetActive(false);
	}

	public void Button_CloseDetail()
	{
		group_Detail.SetActive(false);
		previewCamera.SetActive(false);

		if (previewRoutine != null)
			StopCoroutine(previewRoutine);
		if (previewExeRoutine != null)
			StopCoroutine(previewExeRoutine);

		for (int i = 0; i < 3; i++)
		{
			ParticleSystem particle = pre_Command.GetEffect(i);
			particle.Stop();
			particle.Clear();
		}


		ResetPreview();
	}

	public void Button_ClosePDetail()
	{
		group_PassiveDetail.SetActive(false);
		characterPreviewCamera.SetActive(false);
		if (characterPreviewObj != null)
			Destroy(characterPreviewObj);
	}

	public void Button_CloseError()
	{
		group_Error.SetActive(false);
	}

	public void Button_GoToExitGame()
	{
		group_ExitGame.SetActive(true);
	}

	public void Button_ExitGame()
	{
		Application.Quit();
	}

	public void Button_CloseExitGame()
	{
		group_ExitGame.SetActive(false);
	}

	#endregion

	public void StartSelectGameMode()
	{
		switch (UserInfo.instance.playingGameMode)
		{
			case GameMode.bossRush:
				GoToInGameScene();
				break;
			case GameMode.OneOnOne:
				RequestRandomMatchMaking();
				break;
			default:
				break;
		}
	}

	private void RequestRandomMatchMaking()
	{
		BackendManager.instance.CreateMatchRoom(MatchType.Random, MatchModeType.OneOnOne, randomOneOnOneIndate);
	}

	private void InitializeCommandInfoUI()
	{
		if (!isInit)
			return;
		isInit = false;

		List<CommandId> wholeCommandIdList = new List<CommandId>();
		foreach (CommandId id in Enum.GetValues(typeof(CommandId)))
		{
			wholeCommandIdList.Add(id);
		}

		bool isFirstOne = true;
		bool isFirstClass = true;
		foreach (ClassType cType in Enum.GetValues(typeof(ClassType)))
		{
			#region 전체 커맨드 탭 생성

			GameObject classTapObj = Instantiate(tap, commandListTapParentTr);
			ClassTap classTap = classTapObj.GetComponent<ClassTap>();
			classTapObj.name = cType.ToString();
			classTap.SetStatus(isFirstOne);
			classTap.SetType(cType);
			classTapDic_List[cType] = classTap;

			#endregion

			#region 장착한 커맨드 탭 생성

			if (cType != ClassType.common)
			{
				classTapObj = Instantiate(tap, mountedCommandTapParentTr);
				classTap = classTapObj.GetComponent<ClassTap>();
				classTapObj.name = cType.ToString();
				classTap.SetStatus(isFirstClass);
				classTap.SetType(cType);
				classTap.UpdateMountedInfo();
				classTapDic_Mounted[cType] = classTap;

				var classBlockObj_MountCommon = Instantiate(classBlock, classBlockParentTr_MountCommon);
				classBlockObj_MountCommon.name = cType.ToString();
				classBlockObj_MountCommon.GetComponent<ClassBlock>().SetBlock(cType, ClassChoiceType.mountCommon);

				var classBlockObj_PlayingClass = Instantiate(classBlock, classBlockParentTr_PlayingClass);
				classBlockObj_PlayingClass.name = cType.ToString();
				classBlockObj_PlayingClass.GetComponent<ClassBlock>().SetBlock(cType, ClassChoiceType.selectPlayingClass);
			}

			#endregion

			#region 전체 커맨드 리스트 생성

			GameObject commandListObj = Instantiate(commandListScroll, commandListScrollParentTr);
			commandListObj.SetActive(isFirstOne);
			commandListObj.name = cType.ToString() + "Commands";
			commandListObjDic[cType] = commandListObj;

			Transform contentTr = commandListObj.transform.GetChild(0).GetChild(0);
			if (!cType.Equals(ClassType.common))
			{
				var passiveBlockObj = Instantiate(passiveBlock, contentTr);
				passiveBlockObj.name = cType.ToString() + "Passive";
				var passive = passiveBlockObj.GetComponent<PassiveBlock>();
				passive.SetBlock(cType);
			}

			foreach (CommandId id in UserInfo.instance.ownCommands[cType])
			{
				Command command = Command.FromId(id);
				wholeCommandIdList.Remove(id);

				GameObject commandObj = Instantiate(commandInfoBlock, contentTr);
				commandObj.name = id.ToString();
				CommandInfoBlock infoBlock = commandObj.GetComponent<CommandInfoBlock>();
				infoBlock.SetBlock(command, 0, true);
				infoBlock.SetOwn(true);
			}

			foreach (CommandId id in wholeCommandIdList)
			{
				Command command = Command.FromId(id);
				if (command.classType == cType && command.id != CommandId.Empty)
				{
					GameObject commandObj = Instantiate(commandInfoBlock, contentTr);
					commandObj.name = id.ToString();
					CommandInfoBlock infoBlock = commandObj.GetComponent<CommandInfoBlock>();
					infoBlock.SetBlock(command, 0, false);
					infoBlock.SetOwn(false);
				}
			}

			#endregion

			#region 장착한 커맨드 리스트 생성

			if (cType != ClassType.common)
			{
				GameObject mountedListObj = Instantiate(mountedListScroll, mountedListScrollParentTr);
				mountedListObj.SetActive(isFirstClass);
				mountedListObj.name = cType.ToString();
				mountedListObjDic[cType] = mountedListObj;

				contentTr = mountedListObj.transform.GetChild(0).GetChild(0);

				foreach (CommandId id in UserInfo.instance.mountedCommands[cType])
				{
					Command command = Command.FromId(id);

					GameObject commandObj = Instantiate(commandInfoBlock, contentTr);
					CommandInfoBlock block = commandObj.GetComponent<CommandInfoBlock>();
					commandObj.name = id.ToString();
					block.SetBlock(command, 1, true);
					if (command.classType.Equals(ClassType.common))
						block.mountedCType = cType;
				}

				isFirstClass = false;
			}

			#endregion

			isFirstOne = false;
		}
	}

	public void MountCommon(CommandId id)
	{
		group_MountCommon.SetActive(true);
		commonCommandIdForWaiting = id;
	}

	public void InstantiateMountedCommand(ClassType cType, CommandId id)
	{
		Transform parent = mountedListScrollParentTr.Find(cType.ToString()).GetChild(0).GetChild(0);
		GameObject commandObj = Instantiate(commandInfoBlock, parent);
		commandObj.name = id.ToString();
		CommandInfoBlock block = commandObj.GetComponent<CommandInfoBlock>();
		block.SetBlock(Command.FromId(id), 1, true);
		block.mountedCType = cType;
	}

	public void InstantiateAttackRange(Who commander, (int x, int y) pos, float destroyTime)
	{
		Vector3 vec3 = pre_Grid.PosToVec3(pos) + Vector3.up * 0.1f;
		var obj = Instantiate(pre_AttackRange, vec3, Quaternion.identity);
		pre_ObjList.Add(obj);
		Destroy(obj, destroyTime);
	}

	public void InstantiateDamageTMP(Transform tr, string message, int mode, bool isMultiple = false)
	{
		var obj = Instantiate(damageText);
		obj.transform.position = tr.position + Vector3.up * 3.75f;
		var tmp = obj.GetComponent<DamageTMP>();
		tmp.message = message;
		tmp.SetEffect(mode, isMultiple);
		pre_ObjList.Add(obj);
		pre_TMPList.Add(tmp);
	}

	private void Loading(bool state, ForWhat what)
	{
		group_Loading.SetActive(state);
		string loadingMessage = "";
		button_CancelMatchmaking.SetActive(false);

		switch (what)
		{
			case ForWhat.joinMatchingServer:
				loadingMessage = "매칭서버 접속 중...";
				break;
			case ForWhat.matchMaking:
				loadingMessage = "상대를 찾는 중...";
				button_CancelMatchmaking.SetActive(true);
				break;
			case ForWhat.joinGameServer:
				loadingMessage = "게임서버 접속 중...";
				break;
			case ForWhat.joinGameRoom:
				loadingMessage = "게임룸 입장...";
				break;
		}
		text_LoadingMessage.text = loadingMessage;
	}

	public void GetError(string message, ForWhat what)
	{
		Debug.Log(what.ToString() + " 에러: " + message);
		group_Error.SetActive(true);
		text_ErrorMessage.text = message;
	}

	private void GoToInGameScene()
	{
		UserInfo.instance.RemoveAllHandler();
		StartCoroutine(LoadGameSceneAsync());
	}

	private IEnumerator LoadGameSceneAsync()
	{
		Loading(false, ForWhat.none);
		image_Blind.gameObject.SetActive(true);
		image_Blind.DOFade(1f, 1f);
		yield return new WaitForSeconds(1f);

		SceneManager.LoadScene("InGame");
	}
}
