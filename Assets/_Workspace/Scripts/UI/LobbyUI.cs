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
	public GameObject group_Loading;
	public Text text_LoadingMessage;

	public GameObject group_Main;

	public GameObject group_Item;

	public GameObject group_CommandList;
	public GameObject arrow_CommandList;

	public GameObject group_MountedCommand;
	public GameObject arrow_MountedList;

	public GameObject group_MountCommon;

	public GameObject group_PlayingClass;

	public bool isInit = true;


	public Transform commandListTapParentTr;
	public Transform mountedCommandTapParentTr;
	public GameObject tap;
	public Transform commandListScrollParentTr;
	public GameObject commandListScroll;
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
	private BuffSet pre_BuffSet1;
	private BuffSet pre_BuffSet2;
	private GameObject pre_P1Obj;
	private GameObject pre_P2Obj;

	#region LifeCycle

	private void Start()
	{
		UserInfo.instance.UpdateCommandInfo();
		BackendManager.instance.JoinMatchingServer();
		AddHandler();
		SetPreview();
		UpdateCommandInfoUI();
	}

	private void LateUpdate()
	{
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
	}

	private void RemoveHandler()
	{
		BackendManager.instance.LoadingEvent -= Loading;
		BackendManager.instance.ErrorEvent -= GetError;
		BackendManager.instance.GameStartEvent -= GoToInGameScene;
	}

	#endregion

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
		WaitForSeconds wait1 = new WaitForSeconds(pre_Command.time);
		WaitForSeconds wait2 = new WaitForSeconds(1f);

		while (true)
		{
			pre_Player.SetPos(pre_Command.previewPos.player);
			pre_Player.tr.position = pre_Grid.PosToVec3(pre_Command.previewPos.player);
			pre_Enemy.SetPos(pre_Command.previewPos.enemy);
			pre_Enemy.tr.position = pre_Grid.PosToVec3(pre_Command.previewPos.enemy);
			pre_Player.LookEnemy();
			pre_Enemy.tr.LookAt(pre_Player.tr);

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
		pre_ObjList.Clear();
	}

	public void Button_StartRandomMatchMaking()
	{
		BackendManager.instance.JoinMatchingServer();
		group_PlayingClass.SetActive(true);
	}

	public void RequestMatchMaking()
	{
		BackendManager.instance.RequestMatchMaking(MatchType.Random, MatchModeType.OneOnOne);
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

		ParticleSystem particle = pre_Command.GetEffect();
		particle.Stop();
		particle.Clear();

		ResetPreview();
	}

	private void UpdateCommandInfoUI()
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

	public void InstantiateDamageTMP(Transform tr, string message, int mode)
	{
		var obj = Instantiate(damageText);
		obj.transform.position = tr.position + Vector3.up * 3.75f;
		var tmp = obj.GetComponent<DamageTMP>();
		tmp.message = message;
		tmp.SetEffect(mode);
		pre_ObjList.Add(obj);
		pre_TMPList.Add(tmp);
	}

	private void Loading(bool state, ForWhat what)
	{
		group_Loading.SetActive(state);
		string loadingMessage = "";
		switch (what)
		{
			case ForWhat.joinMatchingServer:
				loadingMessage = "매칭서버 접속 중...";
				break;
			case ForWhat.matchMaking:
				loadingMessage = "상대를 찾는 중...";
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

	private void GetError(string message, ForWhat what)
	{
		Debug.Log(what.ToString() + " 에러: " + message);
	}

	private void GoToInGameScene()
	{
		UserInfo.instance.RemoveAllHandler();
		SceneManager.LoadScene("InGame");
	}
}
