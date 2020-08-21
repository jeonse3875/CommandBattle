﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandSet
{
	public List<Command> commandList = new List<Command>();

	public bool Push(Command command)
	{
		if (GetTotalTime() + command.time > 10)
		{
			return false;
		}
		else
		{
			commandList.Add(command);
			return true;
		}
	}

	public bool Pop()
	{
		if (commandList.Count == 0)
		{
			return false;
		}
		else
		{
			commandList.RemoveAt(commandList.Count - 1);
			return true;
		}
	}

	public void Clear()
	{
		commandList.Clear();
	}

	public List<Command> GetCommandList()
	{
		return commandList;
	}

	public Command[] GetCommandArray()
	{
		Command[] commandArray = new Command[10];
		int index = 0;
		foreach (var command in commandList)
		{
			commandArray[index++] = command;
			int leftTime = command.time - 1;
			for (int i = 0; i < leftTime; i++)
			{
				commandArray[index++] = new EmptyCommand();
			}
		}

		return commandArray;
	}

	public int GetTotalTime()
	{
		int totalTime = 0;
		foreach (var command in commandList)
		{
			totalTime += command.time;
		}

		return totalTime;
	}

	public CommandCompleteMsg ToCCM()
	{
		CommandCompleteMsg ccm = new CommandCompleteMsg();
		Command[] commandArray = GetCommandArray();
		List<CommandId> idList = new List<CommandId>();
		List<Direction> dirList = new List<Direction>();

		foreach (var command in commandArray)
		{
			idList.Add(command.id);
			dirList.Add(command.dir);
		}

		ccm.commandIdList = idList;
		ccm.dirList = dirList;

		return ccm;
	}

	public int GetCommandCount(CommandId id)
	{
		int count = 0;
		foreach(var command in commandList)
		{
			if (command.id.Equals(id))
				count++;
		}
		return count;
	}
}

public enum CommandId
{
	Empty, Move, EarthStrike, WhirlStrike, Guard
}

public enum ClassType
{
	common, knight,
}

public class Command
{
	public Who commander;
	public CommandId id;
	public string name;
	public string description;
	public int time;
	public int limit;
	public int totalDamage;
	public DirectionType dirType;
	public ClassType classType;

	public Direction dir;

	public bool isPreview;
	public ((int x, int y) player, (int x, int y) enemy) previewPos = ((1, 2), (3, 2));
	public Grid pre_grid;
	public PlayerInfo pre_Player;
	public PlayerInfo pre_Enemy;
	public Transform pre_Particles;


	public Command(CommandId id, string name, int time, int limit, int totalDamage, DirectionType dirType, ClassType classType)
	{
		this.id = id;
		this.name = name;
		this.time = time;
		this.limit = limit;
		this.totalDamage = totalDamage;
		this.dirType = dirType;
		this.classType = classType;

		isPreview = false;
	}

	public void SetPreview(Grid grid, PlayerInfo player, PlayerInfo enemy, Transform particles)
	{
		pre_grid = grid;
		pre_Player = player;
		pre_Enemy = enemy;
		pre_Particles = particles;
	}

	public virtual IEnumerator Execute()
	{
		yield break;
	}

	public PlayerInfo GetCommanderInfo()
	{
		if (isPreview)
			return pre_Player;
		return InGame.instance.playerInfo[commander];
	}

	public PlayerInfo GetEnemyInfo()
	{
		if (isPreview)
			return pre_Enemy;
		return InGame.instance.playerInfo[Enemy()];
	}

	public Grid GetGrid()
	{
		if (isPreview)
			return pre_grid;
		return InGame.instance.grid;
	}

	public Who Enemy()
	{
		if (commander == Who.p1)
			return Who.p2;
		else
			return Who.p1;
	}

	public bool CheckEnemyInArea(List<(int x, int y)> area)
	{
		(int x, int y) enemyPos = GetEnemyInfo().Pos();
		foreach (var pos in area)
		{
			if (enemyPos == pos)
				return true;
		}
		return false;
	}

	public List<(int x, int y)> CalculateArea(List<(int x, int y)> area, (int x, int y) curPos, Direction dir = Direction.up)
	{
		Grid grid = GetGrid();
		area = grid.SwitchDirList(area, dir);
		for (int i = 0; i < area.Count; i++)
		{
			area[i] = grid.AddPos(area[i], curPos);
		}

		return area;
	}

	public void SetAnimState(AnimState state)
	{
		GetCommanderInfo().SetAnimState(state);
	}

	public ParticleSystem GetEffect()
	{
		if(isPreview)
		{
			Transform particleTr = pre_Particles.Find(id.ToString());
			if (particleTr != null)
				return particleTr.GetComponent<ParticleSystem>();
			else
				return pre_Particles.GetChild(0).GetComponent<ParticleSystem>();
		}
		else
		{
			Transform particleTr = InGame.instance.particles[commander].Find(id.ToString());
			if (particleTr != null)
				return particleTr.GetComponent<ParticleSystem>();
			else
				return InGame.instance.particles[commander].GetChild(0).GetComponent<ParticleSystem>();
		}
		
	}

	public static Command FromId(CommandId id, Direction dir = Direction.right)
	{
		Type type = Type.GetType(id.ToString() + "Command");
		object[] args = { dir };
		Command instance = Activator.CreateInstance(type, args) as Command;
		return instance;
	}

	public static Command FromString(string name, Direction dir = Direction.right)
	{
		CommandId id = (CommandId)Enum.Parse(typeof(CommandId), name);
		return FromId(id);
	}

	public static string GetKoreanClassName(ClassType cType)
	{
		string className = "";

		switch (cType)
		{
			case ClassType.common:
				className = "공용";
				break;
			case ClassType.knight:
				className = "기사";
				break;
			default:
				break;
		}

		return className;
	}

	public void DisplayAttackRange(List<(int x, int y)> area, float destroyTime)
	{
		if (isPreview)
		{
			LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
			Grid grid = GetGrid();
			foreach (var pos in area)
			{
				if (grid.IsInGrid(pos))
				{
					lobby.InstantiateAttackRange(commander, pos, destroyTime);
				}
			}
		}
		else
		{
			Grid grid = GetGrid();
			foreach (var pos in area)
			{
				if (grid.IsInGrid(pos))
				{
					InGame.instance.InstantiateAttackRange(commander, pos, destroyTime);
				}
			}
		}
	}

	public Sprite GetCommandIcon()
	{
		return Resources.Load<Sprite>("CommandIcon/" + id.ToString());
	}

	public void BattleLog(string message)
	{
		if (isPreview)
			return;
		InGame.instance.inGameUI.InstantiateBattleLog(this, message);
	}

	public void ApplyBuff(Who who, Buff buff)
	{
		if (isPreview)
			return;

		InGame.instance.buffSet[who].Add(buff);
	}
}

public class EmptyCommand : Command
{
	public EmptyCommand(Direction dir = Direction.right)
		: base(CommandId.Empty, "Empty", 1, 10, 0, DirectionType.none, ClassType.common)
	{

	}

	public override IEnumerator Execute()
	{
		Debug.Log(string.Format("[{0}] 대기", commander.ToString()));
		if (GetCommanderInfo().lastAnimState.Equals(AnimState.run))
			SetAnimState(AnimState.idle);
		yield break;
	}
}

public class MoveCommand : Command
{
	public MoveCommand(Direction dir = Direction.right)
		: base(CommandId.Move, "이동", 1, 10, 0, DirectionType.cross, ClassType.common)
	{
		this.dir = dir;
		description = "한 칸 이동합니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		BattleLog(string.Format("{0} 한 칸 이동.", Grid.DirToKorean(dir)));

		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();
		Transform tr = player.tr;

		(int x, int y) curPos = player.Pos();
		(int x, int y) targetPos = grid.SwitchDir((0, 1), dir);
		targetPos = grid.ClampPos(grid.AddPos(curPos, targetPos));

		if (targetPos == curPos || targetPos == enemy.Pos())
		{
			Debug.Log("가로막힘");
			yield break;
		}

		Vector3 startPosVec = tr.position;
		Vector3 targetPosVec = grid.PosToVec3(targetPos);

		float progress = 0f;
		SetAnimState(AnimState.run);
		tr.LookAt(targetPosVec);
		while (progress <= time)
		{
			progress += Time.deltaTime;
			tr.position = Vector3.Lerp(startPosVec, targetPosVec, Mathf.Clamp(progress / (float)time, 0f, 1f));
			player.SetPos(grid.Vec3ToPos(tr.position));
			yield return null;
		}
	}
}

public class EarthStrikeCommand : Command
{
	public EarthStrikeCommand(Direction dir = Direction.right)
		: base(CommandId.EarthStrike, "대지의 일격", 2, 3, 30, DirectionType.cross, ClassType.knight)
	{
		this.dir = dir;
		description = "대지를 강타해 전방의 적에게 피해를 주고 기절시킵니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		BattleLog(string.Format("{0} 공격.", Grid.DirToKorean(dir)));

		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 1), (0, 2) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);

		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		ParticleSystem effect = GetEffect();
		effect.transform.position = grid.PosToVec3(player.Pos());
		effect.transform.LookAt(grid.PosToVec3(attackArea[0]));

		// 수치 조정
		int damage = 30;

		SetAnimState(AnimState.earthStrike);
		DisplayAttackRange(attackArea, 0.4f);
		yield return new WaitForSeconds(0.3f);
		effect.Play();
		yield return new WaitForSeconds(0.25f);
		if (CheckEnemyInArea(attackArea))
		{
			enemy.GetDamage(damage);
			ApplyBuff(enemy.me, new Buff(BuffCategory.stiff, false, 1f));
			BattleLog(string.Format("{0}의 피해를 주고 경직시킴.", damage.ToString()));
		}
		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);
	}
}

public class WhirlStrikeCommand : Command
{
	public WhirlStrikeCommand(Direction dir = Direction.right)
		: base(CommandId.WhirlStrike, "회오리 타격", 1, 5, 20, DirectionType.none, ClassType.knight)
	{
		description = "칼을 휘둘러 주변의 적을 빠르게 공격합니다.";
		previewPos = ((2, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		BattleLog("공격");

		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 1), (0, -1), (1, 1), (1, 0), (1, -1), (-1, 1), (-1, 0), (-1, -1) };
		attackArea = CalculateArea(attackArea, player.Pos());

		ParticleSystem effect = GetEffect();
		effect.transform.position = grid.PosToVec3(player.Pos()) + new Vector3(0, 1.5f, 0);

		//수치 조정
		int damage = 20;
		SetAnimState(AnimState.whirlStrike);
		DisplayAttackRange(attackArea, 0.6f);
		yield return new WaitForSeconds(0.5f);
		effect.Play();
		yield return new WaitForSeconds(0.15f);
		if (CheckEnemyInArea(attackArea))
		{
			enemy.GetDamage(damage);
			BattleLog(string.Format("공격 적중. {0}의 피해", damage.ToString()));
		}
		yield return new WaitForSeconds(0.3f);
		SetAnimState(AnimState.idle);
	}
}

public class GuardCommand : Command
{
	public GuardCommand(Direction dir = Direction.right)
		: base(CommandId.Guard, "방어", 2, 10, 0, DirectionType.none, ClassType.common)
	{
		description = "2초 동안 받는 피해를 50% 감소시킵니다.";
	}

	public override IEnumerator Execute()
	{
		BattleLog("시전");
		ApplyBuff(commander, new Buff(BuffCategory.takenDamage, true, 2f, -0.5f));
		SetAnimState(AnimState.guard);
		yield return new WaitForSeconds(1.9f);
		SetAnimState(AnimState.idle);
		yield break;
	}
}