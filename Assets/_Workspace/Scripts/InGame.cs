using BackEnd.Tcp;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
	bossRush, OneOnOne
}

public enum Who
{
	none, p1, p2
}

public enum MapEvent
{
	none = 0, heal, damageBonus,
}

public class InGame : MonoBehaviour
{
	public static InGame instance;
	public InGameUI inGameUI;

	private Dictionary<Who, bool> isGameStart = new Dictionary<Who, bool>();

	public Who me;
	public Dictionary<Who, ClassType> playingCType = new Dictionary<Who, ClassType>();
	public Dictionary<Who, SessionId> sessionId = new Dictionary<Who, SessionId>();
	public Dictionary<Who, (int play, int win)> matchRecord = new Dictionary<Who, (int play, int win)>();
	public Dictionary<Who, bool> isCommandComplete = new Dictionary<Who, bool>();
	public Dictionary<Who, bool> isBattleEnd = new Dictionary<Who, bool>();
	public bool isMapEvent = false;
	public bool isGameEnd = false;

	public Dictionary<Who, List<Command>> commandList = new Dictionary<Who, List<Command>>();

	public string playerNickname;
	public string opponentNickname;

	public List<Who> deadPlayerList = new List<Who>();
	private List<Who> leavePlayerList = new List<Who>();
	private bool isDraw = false;
	private bool isLeaveEnd = false;

	public Dictionary<Who, PlayerInfo> playerInfo = new Dictionary<Who, PlayerInfo>();
	public Grid grid;

	public Dictionary<Who, Transform> particles = new Dictionary<Who, Transform>();
	public Dictionary<CommandId, ParticleSystem> effects = new Dictionary<CommandId, ParticleSystem>();

	public Dictionary<Who, Coroutine> commandRoutine = new Dictionary<Who, Coroutine>();
	public Dictionary<Who, Command> playingCommand = new Dictionary<Who, Command>();

	public GameObject attackRangeBlue;
	public GameObject attackRangeRed;
	public GameObject damageText;

	public Dictionary<Who, BuffSet> buffSet = new Dictionary<Who, BuffSet>();
	public Dictionary<Who, List<Buff>> chainBuffList = new Dictionary<Who, List<Buff>>();
	public Dictionary<Who, List<GameObject>> effectObj = new Dictionary<Who, List<GameObject>>();

	public Dictionary<Who, bool> isCommandHit = new Dictionary<Who, bool>();

	public InGameCamera cam;

	public bool canViewLastBattle = false;

	private bool skipFirstMapEvent = true;

	public MapEvent curMapEvent;
	public (int x, int y) curMapEventPos;

	private BossType curBoss = BossType.none;
	CommandSet bossCommandSet = new CommandSet();
	public int bossStage = 1;

	#region LifeCycle

	private void Start()
	{
		if (instance == null)
			instance = this;
		else
			Destroy(this);

		AddHandler();
		inGameUI = GetComponent<InGameUI>();

		if(UserInfo.instance.playingGameMode.Equals(GameMode.OneOnOne))
		{
			StartCoroutine(OneOnOneGameRoutine());
		}
		else if (UserInfo.instance.playingGameMode.Equals(GameMode.bossRush))
		{
			StartCoroutine(BossRushGameRoutine());
		}
	}

	private void LateUpdate()
	{
		if (playerInfo.ContainsKey(Who.p1) && playerInfo.ContainsKey(Who.p2))
		{
			playerInfo[Who.p1].SetPos(grid.Vec3ToPos(playerInfo[Who.p1].tr.position));
			playerInfo[Who.p2].SetPos(grid.Vec3ToPos(playerInfo[Who.p2].tr.position));
		}

		if (chainBuffList.ContainsKey(Who.p1) && chainBuffList.ContainsKey(Who.p2))
		{
			foreach(var buff in chainBuffList[Who.p1])
			{
				buffSet[Who.p1].Add(buff);
			}

			foreach (var buff in chainBuffList[Who.p2])
			{
				buffSet[Who.p2].Add(buff);
			}

			chainBuffList[Who.p1].Clear();
			chainBuffList[Who.p2].Clear();
		}

		if (buffSet.ContainsKey(Who.p1) && buffSet.ContainsKey(Who.p2))
		{
			buffSet[Who.p1].Update(Time.deltaTime);
			buffSet[Who.p2].Update(Time.deltaTime);
		}

		if (!deadPlayerList.Count.Equals(0) && !isGameEnd)
		{
			if (UserInfo.instance.playingGameMode.Equals(GameMode.OneOnOne))
			{
				isGameEnd = true;
				isDraw = deadPlayerList.Count.Equals(2);
			}
			else
			{
				if (deadPlayerList.Contains(Who.p1))
				{
					isGameEnd = true;
				}
				deadPlayerList.Clear();
			}
		}
	}

	private void OnDestroy()
	{
		RemoveHandler();
	}

	private void AddHandler()
	{
		BackendManager.instance.GetMsgEvent += GetMessage;
		BackendManager.instance.LeaveGameEvent += LeaveGame;
	}

	private void RemoveHandler()
	{
		BackendManager.instance.GetMsgEvent -= GetMessage;
		BackendManager.instance.LeaveGameEvent -= LeaveGame;
	}

	#endregion

	#region 1대1 게임루틴

	private IEnumerator OneOnOneGameRoutine()
	{
		var waitGameStart = new WaitUntil(IsGameStart);
		var waitCommandComplete = new WaitUntil(IsCommandComplete);
		var waitBattleEnd = new WaitUntil(IsBattleEnd);
		var waitMapEvent = new WaitUntil(IsMapEvent);
		var waitIntroduce = new WaitForSeconds(2f);

		if (BackendManager.instance.isP1)
			me = Who.p1;
		else
			me = Who.p2;

		BackendManager.instance.SendData(new GameStartMsg());
		yield return waitGameStart;
		BackendManager.instance.SendData(new GameStartMsg());

		InitializeGame();

		#region 연출

		yield return new WaitForSeconds(inGameUI.DoFadeOut());

		// P1 소개
		yield return new WaitForSeconds(cam.ZoomInTarget(playerInfo[Who.p1].tr.position));
		inGameUI.IntroducePlayer(Who.p1);
		yield return waitIntroduce;
		inGameUI.IntroducePlayer(Who.none);

		// P2 소개
		yield return new WaitForSeconds(cam.ZoomInTarget(playerInfo[Who.p2].tr.position));
		inGameUI.IntroducePlayer(Who.p2);
		yield return waitIntroduce;
		inGameUI.IntroducePlayer(Who.none);

		// 게임 시작
		yield return new WaitForSeconds(cam.ZoomOut());

		#endregion

		while (!isGameEnd)
		{
			InitializeVariable();
			StartMakingCommand();
			yield return waitCommandComplete;
			StartBattle();
			yield return waitBattleEnd;
			if(!isGameEnd)
			{
				SendMapEvent(ChooseMapEvent());
				yield return waitMapEvent;
				yield return StartCoroutine(ApplyMapEvent());
			}
		}
		yield return StartCoroutine(EndGame());
	}

	private void InitializeGame()
	{
		Debug.Log("게임 초기화");
		var record = UserInfo.instance.matchRecord[playingCType[me]];
		record.play++;
		UserInfo.instance.matchRecord[playingCType[me]] = record;

		grid = new Grid(5, 5, 5f, new Vector3(-10, 0, -10));
		playerNickname = BackendManager.instance.GetMyNickname();
		opponentNickname = BackendManager.instance.GetOpponentNickname();
		GameObject obj_P1 = Instantiate(Resources.Load<GameObject>("Character/" + playingCType[Who.p1].ToString() + "_Blue"));
		GameObject obj_P2 = Instantiate(Resources.Load<GameObject>("Character/" + playingCType[Who.p2].ToString() + "_Red"));
		playerInfo[Who.p1] = new PlayerInfo(Who.p1, (0, 2), obj_P1.transform);
		playerInfo[Who.p2] = new PlayerInfo(Who.p2, (4, 2), obj_P2.transform);
		playerInfo[Who.p1].LookEnemy();
		playerInfo[Who.p2].LookEnemy();
		particles[Who.p1] = GameObject.Find("Particles_p1").transform;
		particles[Who.p2] = GameObject.Find("Particles_p2").transform;
		buffSet[Who.p1] = new BuffSet(Who.p1);
		buffSet[Who.p2] = new BuffSet(Who.p2);
		chainBuffList[Who.p1] = new List<Buff>();
		chainBuffList[Who.p2] = new List<Buff>();
		effectObj[Who.p1] = new List<GameObject>();
		effectObj[Who.p2] = new List<GameObject>();
		commandRoutine[Who.p1] = null;
		commandRoutine[Who.p2] = null;
		isCommandHit[Who.p1] = false;
		isCommandHit[Who.p2] = false;
		isCommandComplete[Who.p1] = false;
		isCommandComplete[Who.p2] = false;

		cam.ResetCamera();

		AddPassive(Who.p1);
		AddPassive(Who.p2);

		MapEventPickUp.curPickUpCount = 0;

		inGameUI.InitializeGame();
	}

	private void InitializeVariable()
	{
		isBattleEnd[Who.p1] = false;
		isBattleEnd[Who.p2] = false;
		isMapEvent = false;

		inGameUI.InitializeVariable();
	}

	private void StartMakingCommand()
	{
		Debug.Log("커맨드 만들기 시작");
		inGameUI.StartMakingCommand();
	}

	private void StartBattle()
	{
		isCommandComplete[Who.p1] = false;
		isCommandComplete[Who.p2] = false;

		inGameUI.StartBattle();
		Debug.Log("배틀 시작");
		canViewLastBattle = true;
		StartCoroutine(BattleRoutine());
	}

	private IEnumerator BattleRoutine()
	{
		if (isLeaveEnd)
			yield break;

		yield return new WaitForSeconds(2.5f);

		int endTime = Mathf.Max(GetPlayerEndTime(Who.p1), GetPlayerEndTime(Who.p2));

		if (!commandList.ContainsKey(Who.p1) || !commandList.ContainsKey(Who.p2))
			yield break;

		for (int currentTime = 0; currentTime < 10; currentTime++)
		{
			if (playerInfo[Who.p1].isDead || playerInfo[Who.p2].isDead)
				break;

			if (currentTime.Equals(endTime))
				break;

			if (commandList[Who.p1][currentTime].id.Equals(CommandId.Empty))
				StartCoroutine(commandList[Who.p1][currentTime].Execute());
			else
			{
				if (!commandList[Who.p1][currentTime].totalDamage.Equals(0))
				{
					buffSet[Who.p1].UpdateCount(CountType.tryAttack, -1);
					StartCoroutine(CheckCommandMissed(commandList[Who.p1][currentTime].time, Who.p1));
				}

				if (playerInfo[Who.p1].isPuppet)
					commandList[Who.p1][currentTime].dir = Grid.OppositeDir(commandList[Who.p1][currentTime].dir);
				commandRoutine[Who.p1] = StartCoroutine(commandList[Who.p1][currentTime].Execute());
				playingCommand[Who.p1] = commandList[Who.p1][currentTime];
			}

			if (commandList[Who.p2][currentTime].id.Equals(CommandId.Empty))
				StartCoroutine(commandList[Who.p2][currentTime].Execute());
			else
			{
				if (!commandList[Who.p2][currentTime].totalDamage.Equals(0))
				{
					buffSet[Who.p2].UpdateCount(CountType.tryAttack, -1);
					StartCoroutine(CheckCommandMissed(commandList[Who.p2][currentTime].time, Who.p2));
				}

				if (playerInfo[Who.p2].isPuppet)
					commandList[Who.p2][currentTime].dir = Grid.OppositeDir(commandList[Who.p2][currentTime].dir);
				commandRoutine[Who.p2] = StartCoroutine(commandList[Who.p2][currentTime].Execute());
				playingCommand[Who.p2] = commandList[Who.p2][currentTime];
			}

			#region 커맨드 빗나감 체크를 위해 playingCommand를 Empty로 바꿔줌
			if (GetPlayerEndTime(Who.p1).Equals(currentTime))
			{
				playingCommand[Who.p1] = commandList[Who.p1][currentTime];
			}

			if (GetPlayerEndTime(Who.p2).Equals(currentTime))
			{
				playingCommand[Who.p2] = commandList[Who.p2][currentTime];
			}
			#endregion

			yield return new WaitForSeconds(1f);
		}
		playerInfo[Who.p1].SetAnimState(AnimState.idle);
		playerInfo[Who.p2].SetAnimState(AnimState.idle);
		buffSet[Who.p1].Clear(true);
		buffSet[Who.p2].Clear(true);

		yield return new WaitForSeconds(0.5f);

		if(playerInfo[Who.p1].poisonCount>0)
		{
			int poisonDamage = 10 * playerInfo[Who.p1].poisonCount;
			playerInfo[Who.p1].TakeDamage(poisonDamage, poisonDamage);
		}
		if (playerInfo[Who.p2].poisonCount > 0)
		{
			int poisonDamage = 10 * playerInfo[Who.p2].poisonCount;
			playerInfo[Who.p2].TakeDamage(poisonDamage, poisonDamage);
		}

		if (UserInfo.instance.playingGameMode.Equals(GameMode.OneOnOne))
		{
			yield return StartCoroutine(WerewolfTransform(Who.p1));
			yield return StartCoroutine(WerewolfTransform(Who.p2));

			BackendManager.instance.SendData(new BattleEndMsg());
		}
		else
		{
			yield return StartCoroutine(WerewolfTransform(Who.p1));
			isBattleEnd[Who.p1] = true;
		}

		yield return new WaitForSeconds(2f);
	}

	private void SendMapEvent(MapEventMsg msg)
	{
		if (!me.Equals(Who.p1))
			return;

		BackendManager.instance.SendData(msg);
	}

	private MapEventMsg ChooseMapEvent()
	{
		curMapEvent = MapEvent.none;
		MapEventMsg noEvent = new MapEventMsg(MapEvent.none, (10, 10));

		if (!me.Equals(Who.p1))
			return noEvent;

		if (skipFirstMapEvent)
		{
			skipFirstMapEvent = false;
			return noEvent;
		}

		if (UnityEngine.Random.Range(0, 1f) > 0.65f)
			return noEvent;

		if (MapEventPickUp.curPickUpCount >= 1)
			return noEvent;

		int eventCount = Enum.GetValues(typeof(MapEvent)).Length;
		int eventIndex = UnityEngine.Random.Range(1, eventCount);
		curMapEvent = (MapEvent)eventIndex;
		List<(int x, int y)> playersPos = new List<(int x, int y)> { playerInfo[Who.p1].Pos(), playerInfo[Who.p2].Pos() };
		curMapEventPos = grid.ChooseRandomPos(playersPos);

		Debug.Log(curMapEvent.ToString() + curMapEventPos.ToString());

		return new MapEventMsg(curMapEvent, curMapEventPos);
	}

	private IEnumerator ApplyMapEvent()
	{
		if (isLeaveEnd)
			yield break;

		GameObject pickUp = Resources.Load<GameObject>(string.Format("MapEvent/MapEvent_{0}", curMapEvent.ToString()));
		Vector3 targetVec = grid.PosToVec3(curMapEventPos);
		if (pickUp != null)
			pickUp = Instantiate(pickUp, targetVec + Vector3.up * 23f, Quaternion.identity);
		
		switch (curMapEvent)
		{
			case MapEvent.heal:
			case MapEvent.damageBonus:
				pickUp.transform.DOMove(targetVec, 2f).SetEase(Ease.OutCubic);
				pickUp.GetComponent<MapEventPickUp>().SetPickUp(curMapEvent, curMapEventPos);
				yield return new WaitForSeconds(2.3f);
				break;
			default:
				break;
		}

	}

	private bool IsGameStart()
	{
		if (isGameStart.ContainsKey(Who.p1) && isGameStart.ContainsKey(Who.p2))
			return isGameStart[Who.p1] && isGameStart[Who.p2];
		else
			return false;
	}

	private bool IsCommandComplete()
	{
		CheckSomeoneLeaveGame();
		return isCommandComplete[Who.p1] && isCommandComplete[Who.p2];
	}

	private bool IsBattleEnd()
	{
		CheckSomeoneLeaveGame();
		return isBattleEnd[Who.p1] && isBattleEnd[Who.p2];
	}

	private bool IsMapEvent()
	{
		CheckSomeoneLeaveGame();
		return isMapEvent;
	}

	private IEnumerator EndGame()
	{
		Debug.Log("게임 종료");

		MatchGameResult result = new MatchGameResult();
		result.m_winners = new List<SessionId>();
		result.m_losers = new List<SessionId>();
		result.m_draws = new List<SessionId>();

		if (isDraw)
		{
			result.m_draws.Add(sessionId[Who.p1]);
			result.m_draws.Add(sessionId[Who.p2]);
			BackendManager.instance.GameEnd(result);

			inGameUI.SetMatchResultUI(false, true);
		}
		else
		{
			Who loser = deadPlayerList[0];
			Who winner;
			if (loser == Who.p1)
				winner = Who.p2;
			else
				winner = Who.p1;
			result.m_winners.Add(sessionId[winner]);
			result.m_losers.Add(sessionId[loser]);
			BackendManager.instance.GameEnd(result);

			playerInfo[winner].tr.LookAt(grid.PosToVec3((2, -1)));
			playerInfo[winner].SetAnimState(AnimState.winner);
			yield return new WaitForSeconds(cam.ZoomInTarget(playerInfo[me].tr.position));
			inGameUI.SetMatchResultUI(winner.Equals(me), false, isLeaveEnd);
			if (winner.Equals(me))
			{
				var record = UserInfo.instance.matchRecord[playingCType[me]];
				record.win++;
				UserInfo.instance.matchRecord[playingCType[me]] = record;
			}
		}
		
	}

	#endregion

	#region 서브루틴

	private void AddPassive(Who who)
	{
		if (playerInfo[who].specialize == null)
			return;

		var passives = playerInfo[who].specialize.GetPassive();

		if (passives == null)
			return;

		foreach (var passive in passives)
		{
			passive.isPassive = true;
			buffSet[who].Add(passive);
		}
	}

	private int GetPlayerEndTime(Who who)
	{
		int index = 0;
		while (index < 10)
		{
			if (!commandList.ContainsKey(who))
				break;

			if (commandList[who][index].id.Equals(CommandId.Empty))
			{
				break;
			}
			else
			{
				index += commandList[who][index].time;
			}
		}

		return index;
	}

	private IEnumerator CheckCommandMissed(int commandTime, Who who)
	{
		yield return new WaitForSeconds(commandTime - 0.01f);
		if (isCommandHit[who].Equals(true))
		{
			playerInfo[who].Resource += playerInfo[who].resourceByHit;
		}
		else
		{
			playerInfo[who].Resource += playerInfo[who].resourceByMiss;
		}

		isCommandHit[who] = false;
	}

	private IEnumerator WerewolfTransform(Who who)
	{
		bool canTransform = playingCType[who].Equals(ClassType.werewolf)
			&& playerInfo[who].Resource >= 3 && playerInfo[who].transformCount.Equals(0) && playerInfo[who].HP > 0;

		if (!canTransform)
			yield break;

		if (isGameEnd)
			yield break;

		yield return new WaitForSeconds(cam.ZoomInTarget(playerInfo[who].tr.position));
		playerInfo[who].SetAnimState(AnimState.innerWildness);
		yield return new WaitForSeconds(0.5f);
		playerInfo[who].specialize.Transform();
		yield return new WaitForSeconds(0.5f);
		playerInfo[who].SetAnimState(AnimState.idle);
		yield return new WaitForSeconds(cam.ZoomOut());

		playerInfo[who].transformCount++;
	}

	private void CheckSomeoneLeaveGame()
	{
		if (!leavePlayerList.Count.Equals(0))
		{
			isCommandComplete[Who.p1] = true;
			isCommandComplete[Who.p2] = true;
			isBattleEnd[Who.p1] = true;
			isBattleEnd[Who.p2] = true;
			isMapEvent = true;

			deadPlayerList.Add(leavePlayerList[0]);
			isGameEnd = true;
			isDraw = false;
			isLeaveEnd = true;
		}
	}

	private void LeaveGame(SessionId leaveId)
	{
		if (sessionId[Who.p1].Equals(leaveId))
			leavePlayerList.Add(Who.p1);
		else
			leavePlayerList.Add(Who.p2);
	}

	public bool StopCommand(Who who, bool forceStop = false)
	{
		if (!playerInfo[who].isUnstoppable || forceStop)
		{
			if (commandRoutine[who] != null)
				StopCoroutine(commandRoutine[who]);

			if (playingCommand.ContainsKey(who) && playingCommand[who].movingTween != null)
				playingCommand[who].movingTween.Kill();
			
			return true;
		}

		return false;
	}

	public void DelayDeath(Who who)
	{
		StartCoroutine(DelayDeathRoutine(who));
	}

	private IEnumerator DelayDeathRoutine(Who who)
	{
		yield return null;
		StopCommand(who, true);
		playerInfo[who].animator.SetInteger("state", 3);
	}

	#endregion

	#region 보스러쉬 게임루틴

	private IEnumerator BossRushGameRoutine()
	{
		var waitCommandComplete = new WaitUntil(()=> { return isCommandComplete[Who.p1]; });
		var waitBattleEnd = new WaitUntil(() => { return isBattleEnd[Who.p1]; });
		var waitMapEvent = new WaitUntil(() => { return isMapEvent; });

		me = Who.p1;

		InitializeGame_BossRush();
		yield return new WaitForSeconds(inGameUI.DoFadeOut());

		// P2 소개
		yield return new WaitForSeconds(cam.ZoomInTarget(playerInfo[Who.p1].tr.position));
		inGameUI.IntroducePlayer_BossRush(Who.p1);
		yield return new WaitForSeconds(2f);
		inGameUI.IntroducePlayer_BossRush(Who.none);

		// 게임 시작
		yield return new WaitForSeconds(cam.ZoomOut());

		while (!isGameEnd)
		{
			InitializeVariable_BossRush();
			StartMakingCommand();
			MakeBossCommand();
			yield return waitCommandComplete;
			StartBattle();
			yield return waitBattleEnd;
			if (!isGameEnd)
			{
				if (playerInfo[Who.p2].isDead)
				{
					yield return StartCoroutine(SpawnNextBoss());
				}
				else
				{
					ChooseMapEvent_BossRush();
					yield return StartCoroutine(ApplyMapEvent());
				}
			}
		}
		yield return StartCoroutine(EndGame_BossRush());
	}

	private void InitializeGame_BossRush()
	{
		Debug.Log("보스러쉬 초기화");
		playingCType[Who.p1] = UserInfo.instance.playingClass;
		curBoss = ChooseRandomBoss();

		grid = new Grid(5, 5, 5f, new Vector3(-10, 0, -10));
		playerNickname = BackendManager.instance.nickname;
		opponentNickname = Command.GetKoreanBossName(curBoss);
		GameObject obj_P1 = Instantiate(Resources.Load<GameObject>("Character/" + UserInfo.instance.playingClass.ToString() + "_Blue"));
		GameObject obj_P2 = Instantiate(Resources.Load<GameObject>("Character/Boss/" + curBoss.ToString()));
		playerInfo[Who.p1] = new PlayerInfo(Who.p1, (0, 2), obj_P1.transform);
		playerInfo[Who.p2] = new PlayerInfo(Who.p2, (4, 2), obj_P2.transform);
		playerInfo[Who.p1].LookEnemy();
		playerInfo[Who.p2].LookEnemy();
		particles[Who.p1] = GameObject.Find("Particles_p1").transform;
		particles[Who.p2] = GameObject.Find("Particles_p2").transform;//임시
		buffSet[Who.p1] = new BuffSet(Who.p1);
		buffSet[Who.p2] = new BuffSet(Who.p2);
		chainBuffList[Who.p1] = new List<Buff>();
		chainBuffList[Who.p2] = new List<Buff>();
		effectObj[Who.p1] = new List<GameObject>();
		effectObj[Who.p2] = new List<GameObject>();
		commandRoutine[Who.p1] = null;
		commandRoutine[Who.p2] = null;
		isCommandHit[Who.p1] = false;
		isCommandHit[Who.p2] = false;
		isCommandComplete[Who.p1] = false;
		isCommandComplete[Who.p2] = false;

		cam.ResetCamera();

		AddPassive(Who.p1);

		MapEventPickUp.curPickUpCount = 0;
		MapEventPickUp.totalPickUpCount = 0;

		inGameUI.InitializeGame_BossRush();
	}

	private void InitializeVariable_BossRush()
	{
		isBattleEnd[Who.p1] = false;
		isMapEvent = false;
		bossCommandSet.Clear();

		inGameUI.InitializeVariable();
	}

	private void MakeBossCommand()
	{
		Command testCommand = new EarthStrikeCommand(Direction.left);
		testCommand.commander = Who.p2;
		bossCommandSet.Push(testCommand);
		// CommandSet 채우기

		while (bossCommandSet.GetTotalTime() < 10)
		{
			Command command = new EmptyCommand();
			bossCommandSet.Push(command);
		}
		bossCommandSet.SetCommander(Who.p2);

		var cList = new List<Command>();
		cList.AddRange(bossCommandSet.GetCommandArray());
		commandList[Who.p2] = cList;
	}

	private void ChooseMapEvent_BossRush()
	{
		curMapEvent = MapEvent.none;

		if (UnityEngine.Random.Range(0, 1f) > 0.7f)
			return;
		if (MapEventPickUp.curPickUpCount >= 1)
			return;
		if (MapEventPickUp.totalPickUpCount >= 3 * bossStage)
			return;

		int eventCount = Enum.GetValues(typeof(MapEvent)).Length;
		int eventIndex = UnityEngine.Random.Range(1, eventCount);
		curMapEvent = (MapEvent)eventIndex;
		List<(int x, int y)> playersPos = new List<(int x, int y)> { playerInfo[Who.p1].Pos(), playerInfo[Who.p2].Pos() };
		curMapEventPos = grid.ChooseRandomPos(playersPos);
	}

	private IEnumerator SpawnNextBoss()
	{
		bossStage++;
		var beforeBoss = playerInfo[Who.p2].tr.gameObject;
		foreach (var effect in effectObj[Who.p2])
		{
			if (effect != null)
				Destroy(effect);
		}
		effectObj[Who.p2].Clear();

		curBoss = ChooseRandomBoss();
		opponentNickname = Command.GetKoreanBossName(curBoss);

		GameObject obj = Instantiate(Resources.Load<GameObject>("Character/Boss/" + curBoss.ToString()));
		playerInfo[Who.p2] = new PlayerInfo(Who.p2, (2, 10), obj.transform);
		buffSet[Who.p2] = new BuffSet(Who.p2);
		chainBuffList[Who.p2] = new List<Buff>();

		List<(int x, int y)> spawnPosList = new List<(int x, int y)>() { (0, 2), (2, 4), (4, 2) };
		spawnPosList.Remove(playerInfo[Who.p1].Pos());
		int spawnIndex = UnityEngine.Random.Range(0, spawnPosList.Count);
		(int x, int y) spawnPos = spawnPosList[spawnIndex];
		(int x, int y) startPos = (-5, 2);

		if(spawnPos == (0,2))
		{
			startPos = (-5, 2);
		}
		else if(spawnPos == (2,4))
		{
			startPos = (2, 9);
		}
		else if(spawnPos == (4,2))
		{
			startPos = (9, 2);
		}

		playerInfo[Who.p2].tr.position = grid.PosToVec3(startPos);
		var spawnVec = grid.PosToVec3(spawnPos);
		playerInfo[Who.p2].tr.LookAt(spawnVec);
		playerInfo[Who.p2].tr.DOMove(spawnVec, 3f).SetEase(Ease.Linear);
		playerInfo[Who.p2].SetAnimState(AnimState.run);

		yield return new WaitForSeconds(2.3f);
		Destroy(beforeBoss);
		yield return new WaitForSeconds(0.7f);
		playerInfo[Who.p2].SetAnimState(AnimState.idle);
		yield return new WaitForSeconds(1f);
	}

	private IEnumerator EndGame_BossRush()
	{
		bool isHighScore = bossStage > UserInfo.instance.bossRushRecord[UserInfo.instance.playingClass];
		if (isHighScore)
		{
			UserInfo.instance.bossRushRecord[UserInfo.instance.playingClass] = bossStage;
		}

		yield return new WaitForSeconds(cam.ZoomInTarget(playerInfo[me].tr.position));
		inGameUI.SetMatchResultUI_BossRush(isHighScore);
	}

	private BossType ChooseRandomBoss()
	{
		int bossCount = Enum.GetValues(typeof(BossType)).Length;
		int index;
		do
		{
			index = UnityEngine.Random.Range(1, bossCount);
		} while (((BossType)index).Equals(curBoss));

		return (BossType)index;
	}

	#endregion

	private void GetMessage(string json)
	{
		Msg tempMsg = JsonUtility.FromJson<Msg>(json);
		//Debug.Log(tempMsg.type.ToString() + "수신 : " + json);

		switch (tempMsg.type)
		{
			case Msg.MsgType.gameStart:
				GameStartMsg gsm = JsonUtility.FromJson<GameStartMsg>(json);
				sessionId[gsm.sender] = gsm.sessionId;
				playingCType[gsm.sender] = gsm.cType;
				isGameStart[gsm.sender] = true;
				matchRecord[gsm.sender] = (gsm.play, gsm.win);
				break;
			case Msg.MsgType.commandComplete:
				CommandCompleteMsg ccm = JsonUtility.FromJson<CommandCompleteMsg>(json);
				commandList[ccm.sender] = ccm.ToCommandList();
				isCommandComplete[ccm.sender] = true;
				break;
			case Msg.MsgType.battleEnd:
				BattleEndMsg bem = JsonUtility.FromJson<BattleEndMsg>(json);
				isBattleEnd[bem.sender] = true;
				break;
			case Msg.MsgType.mapEvent:
				MapEventMsg mem = JsonUtility.FromJson<MapEventMsg>(json);
				curMapEvent = mem.mapEvent;
				curMapEventPos.x = mem.x;
				curMapEventPos.y = mem.y;
				isMapEvent = true;
				break;
		}
	}

	#region Instantiate / Destroy

	public void InstantiateAttackRange(Who commander, (int x, int y) pos, float destroyTime)
	{
		Vector3 vec3 = grid.PosToVec3(pos) + Vector3.up * 0.1f;
		if (commander.Equals(Who.p1))
		{
			var obj = Instantiate(attackRangeBlue, vec3, Quaternion.identity);
			Destroy(obj, destroyTime);
		}
		else if (commander.Equals(Who.p2))
		{
			var obj = Instantiate(attackRangeRed, vec3, Quaternion.identity);
			Destroy(obj, destroyTime);
		}
	}

	public void InstantiateDamageTMP(Transform tr, string message, int mode, bool isMultiple = false)
	{
		var obj = Instantiate(damageText);
		obj.transform.position = tr.position + Vector3.up * 3.75f;
		var tmp = obj.GetComponent<DamageTMP>();
		tmp.message = message;
		tmp.SetEffect(mode, isMultiple);
	}

	public GameObject InstantiateTrap(CommandId id)
	{
		GameObject obj = Instantiate(Resources.Load<GameObject>(string.Format("Trap/{0}", id.ToString())));
		return obj;
	}

	public static GameObject InstantiateBuffEffect(Buff buff)
	{
		string name = string.Format("Buff/{0}_{1}_{2}", buff.category.ToString(), buff.isGood.ToString(), buff.buffType.ToString());
		var obj = Resources.Load<GameObject>(name);
		if (obj != null)
			return Instantiate(obj);
		else
			return null;
	}

	public static void DestroyObj(GameObject obj)
	{
		Destroy(obj);
	}

	#endregion

}