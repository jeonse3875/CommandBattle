using DG.Tweening;
using System;
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
		foreach (var command in commandList)
		{
			if (command.id.Equals(id))
				count++;
		}
		return count;
	}
}

public enum ClassType
{
	common, knight, werewolf
}

public enum CommandId
{
	//공용
	Empty, Move, Guard, HealPotion,
	//기사
	EarthStrike, WhirlStrike, CombatReady, EarthWave, Charge,
	//늑대인간
	Cutting, LeapAttack, InnerWildness, HeartRip,
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
	public BuffSet pre_BuffSet1;
	public BuffSet pre_BuffSet2;

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

	public void SetPreview(Grid grid, PlayerInfo player, PlayerInfo enemy, Transform particles, (BuffSet set1, BuffSet set2) buffSet)
	{
		pre_grid = grid;
		pre_Player = player;
		pre_Enemy = enemy;
		pre_Particles = particles;
		pre_BuffSet1 = buffSet.set1;
		pre_BuffSet2 = buffSet.set2;
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

	public ParticleSystem GetEffect(int num = 0)
	{
		string effectName = id.ToString();
		if (!num.Equals(0))
			effectName += num.ToString();

		if (isPreview)
		{
			Transform particleTr = pre_Particles.Find(effectName);
			if (particleTr != null)
				return particleTr.GetComponent<ParticleSystem>();
			else
				return pre_Particles.GetChild(0).GetComponent<ParticleSystem>();
		}
		else
		{
			Transform particleTr = InGame.instance.particles[commander].Find(effectName);
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
			case ClassType.werewolf:
				className = "늑대인간";
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

	public static Sprite GetClassIcon(ClassType cType)
	{
		return Resources.Load<Sprite>("ClassIcon/" + cType.ToString());
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
		{
			buff.isPreview = true;

			if (who.Equals(Who.p1))
				pre_BuffSet1.Add(buff);
			else
				pre_BuffSet2.Add(buff);
		}
		else
		{
			InGame.instance.buffSet[who].Add(buff);
		}
	}

	public int Hit(int damage, bool isMultiple = false)
	{
		int realDamage = GetCommanderInfo().DealDamage(damage, isMultiple);
		BattleLog(string.Format("{0}의 피해", realDamage.ToString()));
		return realDamage;
	}

	public int Hit(int damage, Buff deBuff, bool isMultiple = false)
	{
		ApplyBuff(Enemy(), deBuff);
		int realDamage = GetCommanderInfo().DealDamage(damage, isMultiple);
		BattleLog(string.Format("{0}의 피해", realDamage.ToString()));
		return realDamage;
	}

	public int Restore(int amount)
	{
		int healAmount = GetCommanderInfo().Restore(amount);
		BattleLog(string.Format("{0} 회복", healAmount.ToString()));
		return healAmount;
	}
}

#region 공용 커맨드

public class EmptyCommand : Command
{
	public EmptyCommand(Direction dir = Direction.right)
		: base(CommandId.Empty, "Empty", 1, 10, 0, DirectionType.none, ClassType.common)
	{

	}

	public override IEnumerator Execute()
	{
		Debug.Log(string.Format("[{0}] 대기", commander.ToString()));
		AnimState lastState = GetCommanderInfo().lastAnimState;
		if (lastState.Equals(AnimState.run) || lastState.Equals(AnimState.stiff))
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
		description = "전방으로 한 칸 이동합니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();
		Transform tr = player.tr;

		(int x, int y) curPos = player.Pos();
		(int x, int y) targetPos = grid.SwitchDir((0, 1), dir);
		targetPos = grid.ClampPos(grid.AddPos(curPos, targetPos));

		if (targetPos == curPos || targetPos == enemy.Pos())
		{
			BattleLog(string.Format("가로막힘", Grid.DirToKorean(dir)));
			yield break;
		}
		else
		{
			BattleLog(string.Format("{0} 이동.", Grid.DirToKorean(dir)));
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
			yield return null;
		}
	}
}

public class GuardCommand : Command
{
	public GuardCommand(Direction dir = Direction.right)
		: base(CommandId.Guard, "방어", 2, 10, 0, DirectionType.none, ClassType.common)
	{
		description = "방어 태세를 취하여 2초 동안 받는 피해를 50% 감소시킵니다.";
	}

	public override IEnumerator Execute()
	{
		BattleLog("2초간 방어 강화");

		Buff damageReduce = new Buff(BuffCategory.takeDamage, true, -0.5f);
		damageReduce.SetDuration(2f);
		ApplyBuff(commander, damageReduce);

		SetAnimState(AnimState.guard);
		yield return new WaitForSeconds(1.9f);
		SetAnimState(AnimState.idle);
		yield break;
	}
}

public class HealPotionCommand : Command
{
	public HealPotionCommand(Direction dir = Direction.right)
		: base(CommandId.HealPotion,"회복 포션",1,1,0,DirectionType.none,ClassType.common)
	{
		description = "비상용 회복 포션을 마셔 체력을 20 회복합니다.";
	}

	public override IEnumerator Execute()
	{
		SetAnimState(AnimState.healPotion);
		GetCommanderInfo().specialize.HideWeapon(0.9f);
		yield return new WaitForSeconds(0.66f);
		Restore(20);
		yield return new WaitForSeconds(0.3f);
		SetAnimState(AnimState.idle);
	}
}


#endregion

#region 기사 커맨드

public class EarthStrikeCommand : Command
{
	public EarthStrikeCommand(Direction dir = Direction.right)
		: base(CommandId.EarthStrike, "대지의 일격", 2, 2, 40, DirectionType.cross, ClassType.knight)
	{
		this.dir = dir;
		description = "대지를 강타해 전방의 적에게 피해를 주고 경직시킵니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{

		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 1), (0, 2) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);

		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		ParticleSystem effect = GetEffect();
		effect.transform.position = grid.PosToVec3(player.Pos());
		effect.transform.LookAt(grid.PosToVec3(attackArea[0]));

		// 수치 조정
		int damage = this.totalDamage;

		SetAnimState(AnimState.earthStrike);
		DisplayAttackRange(attackArea, 0.4f);
		yield return new WaitForSeconds(0.3f);
		effect.Play();
		yield return new WaitForSeconds(0.25f);
		if (CheckEnemyInArea(attackArea))
		{
			Buff stiff = new Buff(BuffCategory.stiff, false);
			stiff.SetCount(CountType.instant);

			Hit(damage, stiff);
		}
		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);
	}
}

public class WhirlStrikeCommand : Command
{
	public WhirlStrikeCommand(Direction dir = Direction.right)
		: base(CommandId.WhirlStrike, "회오리 타격", 1, 3, 25, DirectionType.none, ClassType.knight)
	{
		description = "칼을 휘둘러 주변의 적을 공격합니다.";
		previewPos = ((2, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 1), (0, -1), (1, 1), (1, 0), (1, -1), (-1, 1), (-1, 0), (-1, -1) };
		attackArea = CalculateArea(attackArea, player.Pos());

		ParticleSystem effect = GetEffect();
		effect.transform.position = grid.PosToVec3(player.Pos()) + new Vector3(0, 1.5f, 0);

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.whirlStrike);
		DisplayAttackRange(attackArea, 0.6f);
		yield return new WaitForSeconds(0.5f);
		effect.Play();
		yield return new WaitForSeconds(0.15f);
		if (CheckEnemyInArea(attackArea))
		{
			Hit(damage);
		}
		yield return new WaitForSeconds(0.3f);
		SetAnimState(AnimState.idle);
	}
}

public class CombatReadyCommand : Command
{
	public CombatReadyCommand(Direction dir = Direction.right)
		: base(CommandId.CombatReady, "전투 준비", 2, 1, 0, DirectionType.none, ClassType.knight)
	{
		description = "받는 피해를 한 번만 50% 감소시킵니다. 주는 피해를 한 번만 50% 증가시킵니다.";
	}

	public override IEnumerator Execute()
	{
		Buff takeDamageReduce = new Buff(BuffCategory.takeDamage, true, -0.5f);
		takeDamageReduce.SetCount(CountType.takeDamage, 1);

		Buff dealDamageIncrease = new Buff(BuffCategory.dealDamage, true, +0.5f);
		dealDamageIncrease.SetCount(CountType.dealDamage, 1);
		var effect = GetEffect();
		effect.transform.position = GetCommanderInfo().tr.position;

		// 수치 조정
		SetAnimState(AnimState.combatReady);
		effect.Play();
		yield return new WaitForSeconds(0.8f);
		ApplyBuff(commander, takeDamageReduce);
		ApplyBuff(commander, dealDamageIncrease);
		BattleLog("공격/방어 1회 강화");
		yield return new WaitForSeconds(0.2f);
		SetAnimState(AnimState.idle);

		yield break;
	}
}

public class EarthWaveCommand : Command
{
	public EarthWaveCommand(Direction dir = Direction.right)
		: base(CommandId.EarthWave, "대지 파동", 4, 1, 60, DirectionType.none, ClassType.knight)
	{
		description = "지면에 파동을 일으켜 사방의 적을 공격합니다. 피해를 입은 적은 경직 상태가 됩니다.";
		previewPos = ((2, 2), (4, 0));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea1 = new List<(int x, int y)> { (-1, 1), (1, 1), (-1, -1), (1, -1) };
		List<(int x, int y)> attackArea2 = new List<(int x, int y)> { (-2, 2), (2, 2), (-2, -2), (2, -2) };
		attackArea1 = CalculateArea(attackArea1, player.Pos());
		attackArea2 = CalculateArea(attackArea2, player.Pos());

		ParticleSystem effect1 = GetEffect(1);
		effect1.transform.position = grid.PosToVec3(player.Pos());
		ParticleSystem effect2 = GetEffect(2);
		effect1.transform.position = grid.PosToVec3(player.Pos());
		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.earthWave);
		DisplayAttackRange(attackArea1, 0.63f);
		DisplayAttackRange(attackArea2, 0.63f);

		yield return new WaitForSeconds(0.53f);
		effect1.Play();
		yield return new WaitForSeconds(0.1f);
		if (CheckEnemyInArea(attackArea1))
		{
			Hit(damage, stiff);
		}

		yield return new WaitForSeconds(0.73f);
		effect2.Play();
		yield return new WaitForSeconds(0.1f);
		if (CheckEnemyInArea(attackArea2))
		{
			Hit(damage, stiff);
		}

		yield return new WaitForSeconds(1.1f);
		SetAnimState(AnimState.idle);
	}
}

public class ChargeCommand : Command
{
	public ChargeCommand(Direction dir = Direction.right)
		: base(CommandId.Charge, "돌진", 3, 1, 60, DirectionType.cross, ClassType.knight)
	{
		this.dir = dir;
		description = "3칸 돌진합니다. 부딪힌 적에게 피해를 입히고 자신도 반동 피해를 입습니다. " +
			"공격당한 적은 경직 상태가 되고 한 칸 밀려납니다. 시전 중 저지불가 상태.";
		previewPos = ((0, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();

		SetAnimState(AnimState.charge);

		Buff unStop = new Buff(BuffCategory.unStoppable, true);
		unStop.SetDuration(time - 0.1f);
		ApplyBuff(commander, unStop);

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		var targetPos = grid.SwitchDir((0, 3), dir);
		targetPos = grid.ClampPos(grid.AddPos(targetPos, player.Pos()));
		int distance = grid.DistancePos(player.Pos(), targetPos);

		float endTime = 0.65f * distance;
		var targetVec = grid.PosToVec3(targetPos);
		var moving = player.tr.DOMove(targetVec, endTime).SetEase(Ease.Linear);
		player.tr.LookAt(targetVec);

		var effect = GetEffect();
		effect.transform.position = player.tr.position;
		effect.Play();

		float progress = 0f;
		while(progress < endTime)
		{
			effect.transform.position = player.tr.position;
			if (enemy.Pos().Equals(player.Pos()))
			{
				effect.Stop();
				Hit(totalDamage, stiff);
				moving.Kill();
				player.TakeDamage(20, 20);
				SetAnimState(AnimState.stiff);
				player.tr.LookAt(enemy.tr);
				enemy.tr.LookAt(player.tr);

				var playerBackPos = grid.SwitchDir((0, -1), dir);
				playerBackPos = grid.AddPos(player.Pos(), playerBackPos, true);
				player.tr.DOMove(grid.PosToVec3(playerBackPos), 0.3f).SetEase(Ease.OutCubic);

				var enemyBackPos = grid.SwitchDir((0, 1), dir);
				enemyBackPos = grid.AddPos(enemy.Pos(), enemyBackPos, true);
				enemy.tr.DOMove(grid.PosToVec3(enemyBackPos),0.3f).SetEase(Ease.OutCubic);

				yield return new WaitForSeconds(0.3f);
				break;
			}
			yield return null;
			progress += Time.deltaTime;
		}

		SetAnimState(AnimState.idle);
	}
}
#endregion

#region 늑대인간 커맨드

public class CuttingCommand : Command
{
	public CuttingCommand(Direction dir = Direction.right)
		: base(CommandId.Cutting, "베기 / 할퀴기", 1, 3, 20, DirectionType.cross, ClassType.werewolf)
	{
		this.dir = dir;
		description = "전방의 적을 베어가릅니다. 늑대 상태에서는 더 넓은 범위를 두 번 할퀴어 공격합니다.";
		previewPos = ((2, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		if (player.transformCount.Equals(0))
		{
			List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (-1, 1), (1, 1) };
			attackArea = CalculateArea(attackArea, player.Pos(), dir);
			player.tr.LookAt(grid.PosToVec3(attackArea[0]));

			// 수치조정
			int damage = this.totalDamage;
			SetAnimState(AnimState.cutting);
			DisplayAttackRange(attackArea, 0.28f);
			yield return new WaitForSeconds(0.33f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage);
			}
			yield return new WaitForSeconds(0.6f);
			SetAnimState(AnimState.idle);
		}
		else
		{
			List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (-1, 1), (1, 1), (-1, 0), (1, 0) };
			attackArea = CalculateArea(attackArea, player.Pos(), dir);
			player.tr.LookAt(grid.PosToVec3(attackArea[0]));

			// 수치조정
			int damage = this.totalDamage;
			SetAnimState(AnimState.scratch);
			DisplayAttackRange(attackArea, 0.28f);
			yield return new WaitForSeconds(0.33f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage);
			}
			yield return new WaitForSeconds(0.19f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage);
			}
			yield return new WaitForSeconds(0.4f);
			SetAnimState(AnimState.idle);
		}

		yield break;
	}
}

public class LeapAttackCommand : Command
{
	public LeapAttackCommand(Direction dir = Direction.right)
		: base(CommandId.LeapAttack, "도약 공격", 2, 2, 30, DirectionType.all, ClassType.werewolf)
	{
		this.dir = dir;
		description = "빠르게 도약하여 적을 공격합니다. 늑대 상태에서는 도약 거리가 증가합니다. 대각 방향으로도 사용이 가능합니다.";
		previewPos = ((0, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		if (player.transformCount.Equals(0))
		{
			BattleLog("1칸 도약");

			List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 2) };
			attackArea = CalculateArea(attackArea, player.Pos(), dir);
			player.tr.LookAt(grid.PosToVec3(attackArea[0]));

			(int x, int y) targetPos = (0, 1);
			targetPos = grid.AddPos(grid.SwitchDir(targetPos, dir), player.Pos());
			var targetPosVec = grid.PosToVec3(grid.ClampPos(targetPos));

			// 수치조정
			int damage = this.totalDamage;

			SetAnimState(AnimState.leapAttack);
			player.tr.DOMove(targetPosVec, 0.7f).SetEase(Ease.OutCirc);
			yield return new WaitForSeconds(0.6f);
			DisplayAttackRange(attackArea, 0.35f);
			yield return new WaitForSeconds(0.4f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage);
			}
			yield return new WaitForSeconds(0.37f);
			SetAnimState(AnimState.idle);
		}
		else
		{
			BattleLog("2칸 도약");

			List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 3) };
			attackArea = CalculateArea(attackArea, player.Pos(), dir);
			player.tr.LookAt(grid.PosToVec3(attackArea[0]));

			(int x, int y) targetPos = (0, 2);
			targetPos = grid.AddPos(grid.SwitchDir(targetPos, dir), player.Pos());
			var targetPosVec = grid.PosToVec3(grid.ClampPos(targetPos));

			// 수치조정
			int damage = this.totalDamage;

			SetAnimState(AnimState.leapAttack);
			player.tr.DOMove(targetPosVec, 0.7f).SetEase(Ease.OutCirc);
			yield return new WaitForSeconds(0.6f);
			DisplayAttackRange(attackArea, 0.35f);
			yield return new WaitForSeconds(0.4f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage);
			}
			yield return new WaitForSeconds(0.37f);
			SetAnimState(AnimState.idle);
		}

		yield break;
	}
}

public class InnerWildnessCommand : Command
{
	public InnerWildnessCommand(Direction dir = Direction.right)
		: base(CommandId.InnerWildness, "내면의 야성 / 하울링", 1, 1, 0, DirectionType.none, ClassType.werewolf)
	{
		description = "스스로 20의 피해를 주고, 5초간 적에게 주는 피해량을 30% 증가시킵니다. 늑대 상태에서는 피해를 입지 않습니다.";
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		if (player.transformCount.Equals(0))
		{
			BattleLog("체력 -20. 5초간 공격 강화.");

			Buff dealDamageIncrease = new Buff(BuffCategory.dealDamage, true, +0.3f);
			dealDamageIncrease.SetDuration(5f);
			var effect = GetEffect();
			effect.transform.position = GetCommanderInfo().tr.position;

			// 수치 조정
			SetAnimState(AnimState.innerWildness);
			effect.Play();
			yield return new WaitForSeconds(0.3f);
			player.TakeDamage(20, 20);
			yield return new WaitForSeconds(0.5f);
			ApplyBuff(commander, dealDamageIncrease);
			yield return new WaitForSeconds(0.15f);
			SetAnimState(AnimState.idle);
		}
		else
		{
			BattleLog("5초간 공격 강화.");

			Buff dealDamageIncrease = new Buff(BuffCategory.dealDamage, true, +0.3f);
			dealDamageIncrease.SetDuration(5f);
			var effect = GetEffect();
			effect.transform.position = GetCommanderInfo().tr.position;

			// 수치 조정
			SetAnimState(AnimState.innerWildness);
			effect.Play();
			yield return new WaitForSeconds(0.8f);
			ApplyBuff(commander, dealDamageIncrease);
			yield return new WaitForSeconds(0.15f);
			SetAnimState(AnimState.idle);
		}

		yield break;
	}
}

public class HeartRipCommand : Command
{
	public HeartRipCommand(Direction dir = Direction.right)
		: base(CommandId.HeartRip, "뽑아찢기", 2, 2, 30, DirectionType.cross, ClassType.werewolf)
	{
		this.dir = dir;
		description = "적의 심장을 뽑아 찢습니다. 늑대 상태에서는 입힌 피해량만큼 회복합니다.";
		previewPos = ((2, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (0, 2) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		var effect = GetEffect();

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.heartRip);
		DisplayAttackRange(attackArea, 0.56f);
		yield return new WaitForSeconds(0.66f);
		if (CheckEnemyInArea(attackArea))
		{
			effect.transform.position = GetEnemyInfo().tr.position + Vector3.up * 1.73f;
			effect.Play();
			if (player.transformCount.Equals(0))
			{
				Hit(damage);
			}
			else
			{
				int realDamage = Hit(damage);
				player.Restore(realDamage);
			}
		}
		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);

		yield break;
	}
}

#endregion







