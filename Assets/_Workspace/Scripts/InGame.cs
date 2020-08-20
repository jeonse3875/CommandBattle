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

	public Who me;
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
		var waitCommandComplete = new WaitUntil(IsCommandComplete);
		var waitBattleEnd = new WaitUntil(IsBattleEnd);

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
		BackendManager.instance.SendData(new GameStartMsg());
		Debug.Log("게임 초기화");
		if (BackendManager.instance.isP1)
			me = Who.p1;
		else
			me = Who.p2;
		grid = new Grid(5, 5, 5f, new Vector3(-10, 0, -10));
		playerNickname = BackendManager.instance.GetMyNickname();
		opponentNickname = BackendManager.instance.GetOpponentNickname();
		playerInfo[Who.p1] = new PlayerInfo(Who.p1, (0, 2), GameObject.FindGameObjectWithTag("P1").transform);
		playerInfo[Who.p2] = new PlayerInfo(Who.p2, (4, 2), GameObject.FindGameObjectWithTag("P2").transform);
		playerInfo[Who.p1].LookEnemy();
		playerInfo[Who.p2].LookEnemy();
		particles[Who.p1] = GameObject.Find("Particles_p1").transform;
		particles[Who.p2] = GameObject.Find("Particles_p2").transform;

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
		inGameUI.SetInteractability(true);
		inGameUI.StartTimer();
	}

	private bool IsCommandComplete()
	{
		return isCommandComplete[Who.p1] && isCommandComplete[Who.p2];
	}

	private void StartBattle()
	{
		inGameUI.StopTimer();
		Debug.Log("배틀 시작");
		inGameUI.SetInteractability(false);
		StartCoroutine(BattleRoutine());
	}

	private IEnumerator BattleRoutine()
	{
		for (int currentTime = 0; currentTime < 10; currentTime++)
		{
			Debug.Log(string.Format("[{0}/10]", (currentTime + 1).ToString()));
			if (playerInfo[Who.p1].canAct)
				commandRoutine[Who.p1] = StartCoroutine(commandList[Who.p1][currentTime].Execute());
			if (playerInfo[Who.p2].canAct)
				commandRoutine[Who.p2] = StartCoroutine(commandList[Who.p2][currentTime].Execute());
			yield return new WaitForSeconds(1f);
		}

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
			StopCoroutine(commandRoutine[who]);
	}

	public void InstantiateAttackRange(Who commander, (int x, int y) pos, float destroyTime)
	{
		Vector3 vec3 = grid.PosToVec3(pos) + Vector3.up * 0.1f;
		if(commander.Equals(Who.p1))
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

	public void InstantiateDamageTMP(Vector3 vec3, string message)
	{
		var obj = Instantiate(damageText);
		obj.transform.position = vec3;
		obj.GetComponent<DamageTMP>().message = message;
	}
}