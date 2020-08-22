using BackEnd.Tcp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Who
{
	none, p1, p2
}

public class InGame : MonoBehaviour
{
	public static InGame instance;
	public InGameUI inGameUI;

	private Dictionary<Who, bool> isGameStart = new Dictionary<Who, bool>();

	public Who me;
	public Dictionary<Who, ClassType> playingCType = new Dictionary<Who, ClassType>();
	public Dictionary<Who, SessionId> sessionId = new Dictionary<Who, SessionId>();
	public Dictionary<Who, bool> isCommandComplete = new Dictionary<Who, bool>();
	public Dictionary<Who, bool> isBattleEnd = new Dictionary<Who, bool>();
	public bool isGameEnd = false;

	public Dictionary<Who, List<Command>> commandList = new Dictionary<Who, List<Command>>();

	public string playerNickname;
	public string opponentNickname;

	public List<Who> deadPlayerList = new List<Who>();

	public Dictionary<Who, PlayerInfo> playerInfo = new Dictionary<Who, PlayerInfo>();
	public Grid grid;

	public Dictionary<Who, Transform> particles = new Dictionary<Who, Transform>();
	public Dictionary<CommandId, ParticleSystem> effects = new Dictionary<CommandId, ParticleSystem>();

	public Dictionary<Who, Coroutine> commandRoutine = new Dictionary<Who, Coroutine>();

	public GameObject attackRangeBlue;
	public GameObject attackRangeRed;

	public GameObject damageText;

	public Dictionary<Who, BuffSet> buffSet = new Dictionary<Who, BuffSet>();

	#region LifeCycle
	private void Start()
	{
		if (instance == null)
			instance = this;
		else
			Destroy(this);

		AddHandler();
		inGameUI = GetComponent<InGameUI>();
		StartCoroutine(GameRoutine());
	}

	private void LateUpdate()
	{
		if (buffSet.ContainsKey(Who.p1) && buffSet.ContainsKey(Who.p2))
		{
			buffSet[Who.p1].Update(Time.deltaTime);
			buffSet[Who.p2].Update(Time.deltaTime);
		}
	}

	private void OnDestroy()
	{
		RemoveHandler();
	}

	private void AddHandler()
	{
		BackendManager.instance.GetMsgEvent += GetMessage;
		BackendManager.instance.GameEndEvent += ExitGame;
	}

	private void RemoveHandler()
	{
		BackendManager.instance.GetMsgEvent -= GetMessage;
		BackendManager.instance.GameEndEvent -= ExitGame;
	}
	#endregion

	#region 게임루틴
	private IEnumerator GameRoutine()
	{
		var waitGameStart = new WaitUntil(IsGameStart);
		var waitCommandComplete = new WaitUntil(IsCommandComplete);
		var waitBattleEnd = new WaitUntil(IsBattleEnd);

		BackendManager.instance.SendData(new GameStartMsg());
		yield return waitGameStart;

		InitializeGame();
		while (!isGameEnd)
		{
			InitializeVariable();
			StartMakingCommand();
			yield return waitCommandComplete;
			StartBattle();
			yield return waitBattleEnd;
			CheckGameEnd();
		}
		EndGame();
	}

	private void InitializeGame()
	{
		Debug.Log("게임 초기화");
		if (BackendManager.instance.isP1)
			me = Who.p1;
		else
			me = Who.p2;
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

		inGameUI.InitializeGame();
	}

	private void InitializeVariable()
	{
		isCommandComplete[Who.p1] = false;
		isCommandComplete[Who.p2] = false;
		isBattleEnd[Who.p1] = false;
		isBattleEnd[Who.p2] = false;

		inGameUI.InitializeVariable();
	}

	private void StartMakingCommand()
	{
		Debug.Log("커맨드 만들기 시작");
		inGameUI.SetActiveCommandUI();
		inGameUI.StartTimer();
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
		return isCommandComplete[Who.p1] && isCommandComplete[Who.p2];
	}

	private void StartBattle()
	{
		inGameUI.StopTimer();
		Debug.Log("배틀 시작");
		inGameUI.SetActiveBattleTapUI();
		StartCoroutine(BattleRoutine());
	}

	private IEnumerator BattleRoutine()
	{
		for (int currentTime = 0; currentTime < 10; currentTime++)
		{
			Debug.Log(string.Format("[{0}/10]", (currentTime + 1).ToString()));
			bool canActP1 = playerInfo[Who.p1].canAct;
			bool canActP2 = playerInfo[Who.p2].canAct;

			if (canActP1 && !playerInfo[Who.p1].isDead)
				commandRoutine[Who.p1] = StartCoroutine(commandList[Who.p1][currentTime].Execute());
			else
				playerInfo[Who.p1].canAct = true;
			if (canActP2 && !playerInfo[Who.p2].isDead)
				commandRoutine[Who.p2] = StartCoroutine(commandList[Who.p2][currentTime].Execute());
			else
				playerInfo[Who.p2].canAct = true;

			yield return new WaitForSeconds(1f);
		}
		playerInfo[Who.p1].SetAnimState(AnimState.idle);
		playerInfo[Who.p2].SetAnimState(AnimState.idle);
		buffSet[Who.p1].Clear();
		buffSet[Who.p2].Clear();
		BackendManager.instance.SendData(new BattleEndMsg());
	}

	private bool IsBattleEnd()
	{
		return isBattleEnd[Who.p1] && isBattleEnd[Who.p2];
	}

	private void CheckGameEnd()
	{
		Debug.Log("배틀 종료, 게임 종료 여부 결정");
		if (deadPlayerList.Count != 0)
			isGameEnd = true;
	}

	private void EndGame()
	{
		Debug.Log("게임 종료");

		Who loser = deadPlayerList[0];
		Who winner;
		if (loser == Who.p1)
			winner = Who.p2;
		else
			winner = Who.p1;

		MatchGameResult result = new MatchGameResult();
		result.m_winners = new List<SessionId>();
		result.m_losers = new List<SessionId>();
		result.m_winners.Add(sessionId[winner]);
		result.m_losers.Add(sessionId[loser]);
		BackendManager.instance.GameEnd(result);
	}
	#endregion

	private void ExitGame()
	{
		Debug.Log("게임을 종료하고 로비로 나갑니다.");
		SceneManager.LoadScene("Lobby");
	}

	private void GetMessage(string json)
	{
		Debug.Log("데이터 수신 : " + json);

		Msg tempMsg = JsonUtility.FromJson<Msg>(json);
		Debug.Log(tempMsg.type.ToString());
		switch (tempMsg.type)
		{
			case Msg.MsgType.gameStart:
				GameStartMsg gsm = JsonUtility.FromJson<GameStartMsg>(json);
				sessionId[gsm.sender] = gsm.sessionId;
				playingCType[gsm.sender] = gsm.cType;
				isGameStart[gsm.sender] = true;
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
		}
	}

	public void StopCommand(Who who)
	{
		if (commandRoutine[who] != null)
		{
			StopCoroutine(commandRoutine[who]);
		}
	}

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

	public void InstantiateDamageTMP(Transform tr, string message, int mode)
	{
		var obj = Instantiate(damageText);
		obj.transform.position = tr.position + Vector3.up * 3.75f;
		var tmp = obj.GetComponent<DamageTMP>();
		tmp.message = message;
		tmp.SetEffect(mode);
	}

	public GameObject InstantiateBuffEffect(Buff buff)
	{
		string name = string.Format("Buff/{0}_{1}_{2}", buff.category.ToString(), buff.isGood.ToString(), buff.buffType.ToString());
		var obj = Resources.Load<GameObject>(name);
		if (obj != null)
			return Instantiate(obj);
		else
			return null;
	}

	public void DestroyObj(GameObject obj)
	{
		Destroy(obj);
	}
}