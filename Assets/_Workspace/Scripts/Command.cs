using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
				var emptyCommand = new EmptyCommand();
				emptyCommand.commander = command.commander;
				commandArray[index++] = emptyCommand;
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

	public int GetTotalCost()
	{
		int totalCost = 0;
		foreach (var command in commandList)
		{
			totalCost += command.costResource;
		}

		return totalCost;
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

	public void SetCommander(Who who)
	{
		foreach (var command in commandList)
			command.commander = who;
	}
}

public enum ClassType
{
	common, knight, werewolf, hunter, witch,
}

public enum CommandId
{
	//공용
	Empty = 0, Move, Guard, HealPotion,
	//기사
	EarthStrike, WhirlStrike, CombatReady, EarthWave, Charge, ThornShield,
	//늑대인간
	Cutting, LeapAttack, InnerWildness, HeartRip, Vanish, Sweep,
	//사냥꾼
	RapidShot, FlipShot, StartHunting, HunterTrap, ParalyticArrow, Sniping, HerbTherapy,
	//마녀
	CurseStiff, CursePoison, CursePuppet, SpellFireExplosion, SpellLightning, EscapeSpell,
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
	public int costResource = 0;
	public int price = 0;
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

	public Tween movingTween;
	public (int x, int y) predictPos = (0, 0);
	public bool canPredict = true;

	public BossType bossType = BossType.common;
	public BossCommandId bossId;

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

	public Command(BossCommandId bossId, string name, int time, int totalDamage, DirectionType dirType, BossType bossType)
	{
		this.bossId = bossId;
		this.name = name;
		this.time = time;
		this.totalDamage = totalDamage;
		this.dirType = dirType;
		this.bossType = bossType;
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

	public virtual bool CanUse()
	{
		return true;
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
			case ClassType.hunter:
				className = "사냥꾼";
				break;
			case ClassType.witch:
				className = "마녀";
				break;
			default:
				break;
		}

		return className;
	}

	public static string GetKoreanBossName(BossType bType)
	{
		string name = "";
		switch (bType)
		{
			case BossType.mechGolem:
				name = "메카 골렘";
				break;
			case BossType.demon:
				name = "데몬";
				break;
			default:
				break;
		}

		return name;
	}

	public PlayerInfo GetCommanderInfo()
	{
		if (isPreview)
			return pre_Player;
		return InGame.instance.playerInfo[commander];
	}

	public PlayerInfo GetClientPlayerInfo()
	{
		return InGame.instance.playerInfo[InGame.instance.me];
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

	public bool CheckEnemyInArea((int x, int y) area)
	{
		(int x, int y) enemyPos = GetEnemyInfo().Pos();
		if (enemyPos == area)
			return true;
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

	public (int x, int y) CalculateArea((int x, int y) area, (int x, int y) curPos, Direction dir = Direction.up)
	{
		Grid grid = GetGrid();
		area = grid.SwitchDir(area, dir);
		area = grid.AddPos(curPos, area);

		return area;
	}

	public void SetAnimState(AnimState state)
	{
		GetCommanderInfo().SetAnimState(state);
	}

	public ParticleSystem GetEffect(int num = 0)
	{
		string effectName = id.ToString();
		if (id.Equals(CommandId.Empty))
			effectName = bossId.ToString();
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
		if (!id.Equals(CommandId.Empty))
			return Resources.Load<Sprite>("CommandIcon/" + id.ToString());
		else
			return Resources.Load<Sprite>("CommandIcon/Boss/" + bossId.ToString());
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

	public int Hit(int damage, bool isMultiple = false, bool isBattleLog = true)
	{
		int realDamage = GetCommanderInfo().DealDamage(id, damage, isMultiple);
		if (isBattleLog)
			BattleLog(string.Format("{0}의 피해", realDamage.ToString()));
		if (!isPreview)
		{
			if (InGame.instance.me == commander)
				InGame.instance.totalDamage += realDamage;
		}
		return realDamage;
	}

	public int Hit(int damage, Buff deBuff, bool isMultiple = false)
	{
		ApplyBuff(Enemy(), deBuff);
		int realDamage = GetCommanderInfo().DealDamage(id, damage, isMultiple);
		BattleLog(string.Format("{0}의 피해", realDamage.ToString()));
		if (!isPreview)
		{
			if (InGame.instance.me == commander)
				InGame.instance.totalDamage += realDamage;
		}
		return realDamage;
	}

	public int Restore(int amount)
	{
		int healAmount = GetCommanderInfo().Restore(amount);
		BattleLog(string.Format("{0} 회복", healAmount.ToString()));
		return healAmount;
	}

	public GameObject SetTrap((int x, int y) pos, Who target, int damage, Buff deBuff = null)
	{
		GameObject trap;
		if (isPreview)
		{
			LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
			trap = lobby.InstantiateTrap(id);
		}
		else
		{
			trap = InGame.instance.InstantiateTrap(id);
		}

		trap.GetComponent<Trap>().SetTrap(pos, target, damage, deBuff, isPreview);

		return trap;
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
		predictPos = (0, 1);
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
			BattleLog("가로막힘");
			yield break;
		}
		else if (player.isParalysis)
		{
			BattleLog("마비");
			player.SetAnimState(AnimState.paralysis);
			yield break;
		}
		else if (player.isVanish)
		{
			BattleLog("???");
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
		: base(CommandId.Guard, "방어", 1, 10, 0, DirectionType.none, ClassType.common)
	{
		description = "방어 태세를 취하여 받는 피해를 50% 감소시킵니다.";
	}

	public override IEnumerator Execute()
	{
		BattleLog("방어 강화");

		Buff damageReduce = new Buff(BuffCategory.takeDamage, true, -0.5f);
		damageReduce.SetDuration(1f);
		ApplyBuff(commander, damageReduce);

		GetCommanderInfo().specialize.HideWeapon(0.9f);
		SetAnimState(AnimState.guard);
		yield return new WaitForSeconds(0.95f);
		SetAnimState(AnimState.idle);
		yield break;
	}
}

public class HealPotionCommand : Command
{
	public HealPotionCommand(Direction dir = Direction.right)
		: base(CommandId.HealPotion, "회복 포션", 1, 1, 0, DirectionType.none, ClassType.common)
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
		: base(CommandId.EarthStrike, "대지의 일격", 2, 2, 35, DirectionType.cross, ClassType.knight)
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
		: base(CommandId.WhirlStrike, "회오리 타격", 2, 2, 20, DirectionType.none, ClassType.knight)
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
		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);
	}
}

public class CombatReadyCommand : Command
{
	public CombatReadyCommand(Direction dir = Direction.right)
		: base(CommandId.CombatReady, "전투 준비", 2, 1, 0, DirectionType.none, ClassType.knight)
	{
		description = "받는 피해를 한 번만 30% 감소시킵니다. 주는 피해를 한 번만 30% 증가시킵니다.";
	}

	public override IEnumerator Execute()
	{
		Buff takeDamageReduce = new Buff(BuffCategory.takeDamage, true, -0.3f);
		takeDamageReduce.SetCount(CountType.takeDamage, 1);

		Buff dealDamageIncrease = new Buff(BuffCategory.dealDamage, true, +0.3f);
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
		price = 100;
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
		effect2.transform.position = grid.PosToVec3(player.Pos());
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
		: base(CommandId.Charge, "돌진", 3, 1, 50, DirectionType.cross, ClassType.knight)
	{
		this.dir = dir;
		description = "3칸 돌진합니다. 부딪힌 적에게 피해를 입히고 자신도 반동 피해를 입습니다. " +
			"공격당한 적은 경직 상태가 되고 한 칸 밀려납니다.";
		previewPos = ((0, 2), (3, 2));
		predictPos = (0, 3);
		canPredict = false;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();

		SetAnimState(AnimState.charge);

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		var targetPos = grid.SwitchDir((0, 3), dir);
		targetPos = grid.ClampPos(grid.AddPos(targetPos, player.Pos()));
		int distance = grid.DistancePos(player.Pos(), targetPos);

		float endTime = 0.65f * distance;
		var targetVec = grid.PosToVec3(targetPos);
		movingTween = player.tr.DOMove(targetVec, endTime).SetEase(Ease.Linear);
		player.tr.LookAt(targetVec);

		var effect = GetEffect();
		effect.transform.position = player.tr.position;
		effect.Play();

		float progress = 0f;
		while (progress < endTime)
		{
			effect.transform.position = player.tr.position;
			if (enemy.Pos().Equals(player.Pos()))
			{
				effect.Stop();
				Hit(totalDamage, stiff);
				movingTween.Kill();
				player.TakeDamage(20, 20);

				player.tr.LookAt(enemy.tr);
				player.SetAnimState(AnimState.stiff);
				var playerBackPos = grid.SwitchDir((0, -1), dir);
				playerBackPos = grid.AddPos(player.Pos(), playerBackPos, true);
				player.tr.DOMove(grid.PosToVec3(playerBackPos), 0.3f).SetEase(Ease.OutCubic);

				if (!enemy.isUnstoppable)
				{
					enemy.tr.LookAt(player.tr);

					var enemyBackPos = grid.SwitchDir((0, 1), dir);
					enemyBackPos = grid.AddPos(enemy.Pos(), enemyBackPos, true);
					enemy.tr.DOMove(grid.PosToVec3(enemyBackPos), 0.3f).SetEase(Ease.OutCubic);
				}

				yield return new WaitForSeconds(0.3f);
				break;
			}
			yield return null;
			progress += Time.deltaTime;
		}

		SetAnimState(AnimState.idle);
	}
}

public class ThornShieldCommand : Command
{
	public ThornShieldCommand(Direction dir = Direction.right)
		: base(CommandId.ThornShield, "가시 방패", 2, 1, 0, DirectionType.none, ClassType.knight)
	{
		description = "2초 동안 받는 피해가 40% 감소합니다. 감소시킨 피해를 적에게 반사합니다.";
	}

	public override IEnumerator Execute()
	{
		Buff takeDamageReduce = new Buff(BuffCategory.takeDamage, true, -0.4f);
		takeDamageReduce.SetDuration(2f);

		Buff thorn = new Buff(BuffCategory.thornArmor, true);
		thorn.SetDuration(2f);

		// 수치 조정
		SetAnimState(AnimState.thornShield);
		ApplyBuff(commander, takeDamageReduce);
		ApplyBuff(commander, thorn);
		BattleLog("방어 강화. 피해 일부 반사.");
		yield return new WaitForSeconds(1.95f);
		SetAnimState(AnimState.idle);

		yield break;
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
		var effect = GetEffect();
		Transform enemyTr = GetEnemyInfo().tr;

		if (player.transformCount.Equals(0))
		{
			List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (-1, 1), (1, 1) };
			attackArea = CalculateArea(attackArea, player.Pos(), dir);
			player.tr.LookAt(grid.PosToVec3(attackArea[0]));

			// 수치조정
			int damage = this.totalDamage;
			SetAnimState(AnimState.cutting);
			DisplayAttackRange(attackArea, 0.33f);
			yield return new WaitForSeconds(0.33f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage);
				effect.transform.position = enemyTr.position;
				effect.Play();
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
			DisplayAttackRange(attackArea, 0.33f);
			yield return new WaitForSeconds(0.33f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage, true);
				effect.transform.position = enemyTr.position;
				effect.Play();
			}
			yield return new WaitForSeconds(0.19f);
			if (CheckEnemyInArea(attackArea))
			{
				Hit(damage, true);
				effect.transform.position = enemyTr.position;
				effect.Play();
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
		description = "빠르게 도약하여 적을 공격합니다. 늑대 상태에서는 도약 거리가 증가합니다. 모든 방향으로 사용이 가능합니다.";
		previewPos = ((0, 2), (3, 2));

		if (SceneManager.GetActiveScene().name.Equals("Lobby"))
			return;

		if (GetClientPlayerInfo().transformCount.Equals(0))
			predictPos = (0, 1);
		else
			predictPos = (0, 2);
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
			movingTween = player.tr.DOMove(targetPosVec, 0.7f).SetEase(Ease.OutCirc);
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

public class VanishCommand : Command
{
	public VanishCommand(Direction dir = Direction.right)
		: base(CommandId.Vanish, "은신", 1, 1, 0, DirectionType.none, ClassType.werewolf)
	{
		description = "은신 상태에 돌입합니다. 공격 커맨드를 사용하거나 피해를 입으면 은신이 해제됩니다. " +
			"늑대 상태에서는 은신 해제 후 1초간 주는 피해가 50% 증가합니다.";
	}

	public override bool CanUse()
	{
		return !GetCommanderInfo().isVanish;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();

		var effect = GetEffect();
		effect.transform.position = player.tr.position;
		effect.Play();
		SetAnimState(AnimState.vanish);
		yield return new WaitForSeconds(0.7f);
		SetAnimState(AnimState.idle);

		if (player.transformCount.Equals(0))
		{
			BattleLog("은신");

			Buff vanish = new Buff(BuffCategory.vanish, true);
			List<CountType> countType = new List<CountType>() { CountType.takeDamage, CountType.tryAttack };
			vanish.SetCount(countType);
			vanish.isMultiTurn = true;

			ApplyBuff(commander, vanish);
		}
		else
		{
			BattleLog("은신");

			Buff vanish = new Buff(BuffCategory.vanish, true);
			List<CountType> countType = new List<CountType>() { CountType.takeDamage, CountType.tryAttack };
			vanish.SetCount(countType);
			vanish.isMultiTurn = true;

			Buff dealDamageIncrease = new Buff(BuffCategory.dealDamage, true, +0.5f);
			dealDamageIncrease.SetDuration(1f);

			vanish.SetChainBuff(dealDamageIncrease);
			ApplyBuff(commander, vanish);
		}

		yield break;
	}
}

public class SweepCommand : Command
{
	public SweepCommand(Direction dir = Direction.right)
		: base(CommandId.Sweep, "휩쓸기", 2, 1, 30, DirectionType.cross, ClassType.werewolf)
	{
		this.dir = dir;
		description = "전방으로 두 칸 이동하며 주위의 적을 공격합니다. 늑대 상태에서는 적을 경직시킵니다.";
		previewPos = ((1, 2), (2, 3));
		predictPos = (0, 2);
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();
		var effect = GetEffect();
		Transform enemyTr = GetEnemyInfo().tr;

		List<(int x, int y)> attackArea = new List<(int x, int y)>() { (-1, 1), (1, 1), (0, 2), (-1, 2), (1, 2), (0, 3) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		(int x, int y) targetPos = CalculateArea((0, 2), player.Pos(), dir);
		targetPos = grid.ClampPos(targetPos);
		var targetVec = grid.PosToVec3(targetPos);
		player.tr.LookAt(targetVec);
		effect.transform.position = player.tr.position;
		effect.transform.LookAt(targetVec);

		List<(int x, int y)> area1 = attackArea.GetRange(0, 3);
		List<(int x, int y)> area2 = attackArea.GetRange(3, 3);

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		// 수치조정
		SetAnimState(AnimState.sweep);
		movingTween = player.tr.DOMove(targetVec, 1.3f).SetEase(Ease.Linear);
		DisplayAttackRange(area1, 0.5f);
		yield return new WaitForSeconds(0.5f);
		effect.transform.position = player.tr.position;
		effect.Play();
		if(CheckEnemyInArea(area1))
		{
			if (player.transformCount.Equals(0))
				Hit(totalDamage);
			else
				Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(0.3f);
		DisplayAttackRange(area2, 0.5f);
		yield return new WaitForSeconds(0.5f);
		effect.transform.position = player.tr.position;
		effect.Play();
		if (CheckEnemyInArea(area2))
		{
			if (player.transformCount.Equals(0))
				Hit(totalDamage);
			else
				Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(0.3f);
		SetAnimState(AnimState.idle);
	}
}

#endregion

#region 사냥꾼 커맨드

public class RapidShotCommand : Command
{
	public RapidShotCommand(Direction dir = Direction.right)
		: base(CommandId.RapidShot, "속사", 1, 3, 30, DirectionType.cross, ClassType.hunter)
	{
		this.dir = dir;
		description = "빠른 속도로 활시위를 당겨 먼 거리의 적을 공격합니다.";
		previewPos = ((0, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 1), (0, 2), (0, 3) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		ParticleSystem effect1 = GetEffect(1);
		TrailRenderer trail = effect1.GetComponentInChildren<TrailRenderer>();
		effect1.transform.position = player.tr.position;
		effect1.transform.LookAt(grid.PosToVec3(attackArea[0]));
		trail.Clear();

		ParticleSystem effect2 = GetEffect(2);

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.rapidShot);
		DisplayAttackRange(attackArea, 0.37f);
		yield return new WaitForSeconds(0.33f);

		float duration = 0.3f;
		float progress = 0f;
		effect1.Play();
		effect1.transform.DOMove(grid.PosToVec3(attackArea[attackArea.Count - 1]), duration)
			.SetEase(Ease.OutCubic).OnComplete(() => { effect1.Clear(); effect1.Stop(); });
		var enemy = GetEnemyInfo();
		while (progress < duration)
		{
			var projectilePos = grid.Vec3ToPos(effect1.transform.position);
			if (attackArea.Contains(projectilePos) && projectilePos.Equals(enemy.Pos()))
			{
				Hit(totalDamage);
				effect2.transform.position = enemy.tr.position;
				effect2.Play();
				effect1.Stop();
				effect1.Clear();
				break;
			}
			yield return null;
			progress += Time.deltaTime;
		}

		yield return new WaitForSeconds(0.3f + (duration - progress));
		SetAnimState(AnimState.idle);
	}
}

public class FlipShotCommand : Command
{
	public FlipShotCommand(Direction dir = Direction.right)
		: base(CommandId.FlipShot, "후퇴 사격", 1, 2, 20, DirectionType.cross, ClassType.hunter)
	{
		this.dir = dir;
		description = "전방의 적을 공격하며 뒤로 후퇴합니다.";
		previewPos = ((1, 2), (3, 2));
		predictPos = (0, -1);
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 2), (-1, 2), (1, 2) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));
		var flipPos = (0, -1);
		flipPos = grid.ClampPos(grid.AddPos(player.Pos(), grid.SwitchDir(flipPos, dir)));

		var effect = GetEffect();
		effect.transform.position = grid.PosToVec3(attackArea[0]);
		effect.transform.LookAt(player.tr);

		// 수치조정
		DisplayAttackRange(attackArea, 0.25f);
		yield return new WaitForSeconds(0.25f);
		effect.Play();
		SetAnimState(AnimState.flipShot);
		movingTween = player.tr.DOMove(grid.PosToVec3(flipPos), 0.7f).SetEase(Ease.OutCubic);
		if (CheckEnemyInArea(attackArea))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.6f);

		SetAnimState(AnimState.idle);
	}
}

public class StartHuntingCommand : Command
{
	public StartHuntingCommand(Direction dir = Direction.right)
		: base(CommandId.StartHunting, "사냥 개시", 2, 1, 0, DirectionType.none, ClassType.hunter)
	{
		description = "사냥의 시작을 알립니다. 적이 다음 번에 받는 피해가 두배가 됩니다.";
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		Buff deBuff = new Buff(BuffCategory.takeDamage, false, +1f);
		deBuff.SetCount(CountType.takeDamage);

		SetAnimState(AnimState.startHunting);
		yield return new WaitForSeconds(0.56f);
		ApplyBuff(Enemy(), deBuff);
		BattleLog("적 방어 1회 약화");
		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);
	}
}

public class HunterTrapCommand : Command
{
	public HunterTrapCommand(Direction dir = Direction.right)
		: base(CommandId.HunterTrap, "사냥꾼의 덫", 1, 1, 20, DirectionType.all, ClassType.hunter)
	{
		this.dir = dir;
		description = "밟으면 피해를 입고 경직 상태가 되는 덫을 설치합니다. " +
			"덫은 턴이 끝나도 유지되고 자신은 밟지 않습니다. 모든 방향으로 사용이 가능합니다.";
		previewPos = ((1, 2), (2, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		(int x, int y) pos = (0, 1);
		pos = grid.SwitchDir(pos, dir);
		pos = grid.AddPos(player.Pos(), pos);
		Vector3 vec = grid.PosToVec3(pos);

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		player.tr.LookAt(vec);
		SetAnimState(AnimState.hunterTrap);
		yield return new WaitForSeconds(0.37f);
		BattleLog("덫 설치");
		var trap = SetTrap(pos, Enemy(), totalDamage, stiff);
		trap.transform.position = player.tr.position + Vector3.up * 1.7f;
		float duration = 0.2f;
		trap.transform.DOMoveX(vec.x, duration).SetEase(Ease.OutQuad);
		trap.transform.DOMoveZ(vec.z, duration).SetEase(Ease.OutQuad);
		trap.transform.DOMoveY(0, duration).SetEase(Ease.InSine);
		yield return new WaitForSeconds(0.55f);
		SetAnimState(AnimState.idle);
	}
}

public class ParalyticArrowCommand : Command
{
	public ParalyticArrowCommand(Direction dir = Direction.right)
		: base(CommandId.ParalyticArrow, "마비 화살", 1, 1, 25, DirectionType.all, ClassType.hunter)
	{
		this.dir = dir;
		description = "마비를 일으키는 화살을 발사합니다. 적중당한 적은 마비 상태가 되어 이번 전투 동안 '이동' 커맨드를 사용할 수 없습니다. " +
			"(이동 능력이 있는 다른 커맨드는 사용 가능) 모든 방향으로 사용이 가능합니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 2) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		ParticleSystem effect1 = GetEffect(1);
		TrailRenderer trail = effect1.GetComponentInChildren<TrailRenderer>();
		effect1.transform.position = player.tr.position;
		effect1.transform.LookAt(grid.PosToVec3(attackArea[0]));
		trail.Clear();

		ParticleSystem effect2 = GetEffect(2);

		Buff paralysis = new Buff(BuffCategory.paralysis, false);
		paralysis.SetDuration(10f);

		// 수치조정
		SetAnimState(AnimState.paralyticArrow);
		DisplayAttackRange(attackArea, 0.43f);
		yield return new WaitForSeconds(0.43f);
		effect1.Play();
		effect1.transform.DOMove(grid.PosToVec3(attackArea[0]), 0.15f)
			.SetEase(Ease.OutCubic).OnComplete(() => { effect1.Clear(); effect1.Stop(); });
		yield return new WaitForSeconds(0.1f);
		if (CheckEnemyInArea(attackArea))
		{
			Hit(totalDamage, paralysis);
			effect2.transform.position = GetEnemyInfo().tr.position;
			effect2.Play();
			effect1.Stop();
			effect1.Clear();
		}
		yield return new WaitForSeconds(0.35f);

		SetAnimState(AnimState.idle);
	}
}

public class SnipingCommand : Command
{
	public SnipingCommand(Direction dir = Direction.rightUp)
		: base(CommandId.Sniping, "저격", 3, 1, 25, DirectionType.diagonal, ClassType.hunter)
	{
		this.dir = dir;
		description = "정신을 집중하여 먼 거리까지 날아가는 화살을 발사합니다. " +
			"거리에 비례하여 최대 4배까지 피해량이 증가합니다. 대각 방향으로만 사용이 가능합니다.";
		previewPos = ((0, 0), (4, 4));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		if (isPreview)
			this.dir = Direction.rightUp;

		List<(int x, int y)> attackArea = new List<(int x, int y)> { (0, 1), (0, 2), (0, 3), (0, 4) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		Vector3 targetVec = grid.PosToVec3(attackArea[attackArea.Count - 1]);
		player.tr.LookAt(targetVec);

		ParticleSystem effect1 = GetEffect(1);
		effect1.transform.position = player.tr.position;
		effect1.transform.LookAt(targetVec);
		effect1.Play();

		ParticleSystem effect2 = GetEffect(2);
		TrailRenderer trail = effect2.GetComponentInChildren<TrailRenderer>();
		effect2.transform.position = player.tr.position;
		effect2.transform.LookAt(targetVec);
		trail.Clear();

		ParticleSystem effect3 = GetEffect(3);

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.sniping);
		DisplayAttackRange(attackArea, 1.5f);
		yield return new WaitForSeconds(1.5f);

		float duration = 0.35f;
		float progress = 0f;
		effect2.Play();
		effect2.transform.DOMove(targetVec, duration)
			.SetEase(Ease.OutCubic).OnComplete(() => { effect2.Clear(); effect2.Stop(); });
		var enemy = GetEnemyInfo();
		(int x, int y) projectilePos;
		while (progress < duration)
		{
			projectilePos = grid.Vec3ToPos(effect2.transform.position);
			if (projectilePos.Equals(enemy.Pos()) && attackArea.Contains(projectilePos))
			{
				int mulitplier = Mathf.Abs(grid.SubtractPos(enemy.Pos(), player.Pos()).x);
				Hit(totalDamage * mulitplier);
				effect3.transform.position = enemy.tr.position;
				effect3.Play();
				effect2.Stop();
				effect2.Clear();
				break;
			}
			yield return null;
			progress += Time.deltaTime;
		}

		yield return new WaitForSeconds(0.3f + (duration - progress));
		SetAnimState(AnimState.idle);
	}
}

public class HerbTherapyCommand : Command
{
	public HerbTherapyCommand(Direction dir = Direction.right)
		: base(CommandId.HerbTherapy, "약초 치료", 1, 1, 0, DirectionType.none, ClassType.hunter)
	{
		description = "해로운 효과를 모두 제거하고 체력을 10 회복합니다.";
	}

	public override IEnumerator Execute()
	{
		var player = GetCommanderInfo();
		player.specialize.HideWeapon(0.9f);
		var effect = GetEffect();
		effect.transform.position = player.tr.position;
		effect.Play();
		SetAnimState(AnimState.healPotion);
		yield return new WaitForSeconds(0.66f);
		if (!isPreview)
		{
			int deBuffCount = InGame.instance.buffSet[player.me].ClearBadBuff();
			if (!deBuffCount.Equals(0))
				BattleLog(string.Format("디버프 {0}개 제거", deBuffCount.ToString()));
		}
		Restore(10);
		yield return new WaitForSeconds(0.3f);
		SetAnimState(AnimState.idle);
	}
}

#endregion

#region 마녀 커맨드

public class CurseStiffCommand : Command
{
	public CurseStiffCommand(Direction dir = Direction.right)
		: base(CommandId.CurseStiff, "저주 - 경직", 1, 1, 0, DirectionType.none, ClassType.witch)
	{
		description = "적에게 경직 저주를 겁니다. 저주에 걸린 즉시 경직 상태가 됩니다.";
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		SetAnimState(AnimState.curseStiff);
		player.tr.LookAt(enemy.tr);

		yield return new WaitForSeconds(0.2f);
		var effect = GetEffect();
		effect.transform.position = enemy.tr.position;
		effect.Play();

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);
		ApplyBuff(Enemy(), stiff);
		player.Resource++;

		BattleLog("경직시킴");
		yield return new WaitForSeconds(0.75f);
		SetAnimState(AnimState.idle);
	}
}

public class CursePoisonCommand : Command
{
	public CursePoisonCommand(Direction dir = Direction.right)
		: base(CommandId.CursePoison, "저주 - 중독", 2, 1, 0, DirectionType.cross, ClassType.witch)
	{
		this.dir = dir;
		description = "범위 안의 적에게 중독 저주를 겁니다. " +
			"중독 상태의 적은 배틀이 끝날 때마다 피해를 입습니다. 중첩이 가능합니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();

		List<(int x, int y)> attackArea = new List<(int x, int y)>()
		{ (0,2),(-1,1),(1,1),(-1,3),(1,3) };

		attackArea = CalculateArea(attackArea, player.Pos(), dir);

		var targetVec = GetGrid().PosToVec3(attackArea[0]);
		player.tr.LookAt(targetVec);

		Buff poison = new Buff(BuffCategory.poison, false, 1);
		poison.SetCount(CountType.permanent);
		poison.isMultiTurn = true;

		var effect = GetEffect();

		SetAnimState(AnimState.cursePoison);
		DisplayAttackRange(attackArea, 0.35f);
		yield return new WaitForSeconds(0.35f);
		effect.transform.position = targetVec;
		effect.Play();
		if (CheckEnemyInArea(attackArea))
		{
			ApplyBuff(Enemy(), poison);
			player.Resource++;
			BattleLog(string.Format("중독 적용. 현재 {0}스택", GetEnemyInfo().poisonCount.ToString()));
		}

		yield return new WaitForSeconds(0.6f);
		SetAnimState(AnimState.idle);
	}
}

public class CursePuppetCommand : Command
{
	public CursePuppetCommand(Direction dir = Direction.right)
		: base(CommandId.CursePuppet, "저주 - 꼭두각시", 2, 1, 0, DirectionType.cross, ClassType.witch)
	{
		this.dir = dir;
		description = "범위 안의 적에게 이번 전투 동안 꼭두각시 저주를 겁니다. " +
			"꼭두각시 상태의 적은 커맨드의 방향이 반대로 바뀝니다.";
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();

		List<(int x, int y)> attackArea = new List<(int x, int y)>()
		{ (0,2),(0,1),(0,3),(-1,2),(1,2) };

		attackArea = CalculateArea(attackArea, player.Pos(), dir);

		var targetVec = GetGrid().PosToVec3(attackArea[0]);
		player.tr.LookAt(targetVec);

		Buff puppet = new Buff(BuffCategory.puppet, false);
		puppet.SetDuration(10f);

		var effect = GetEffect();

		SetAnimState(AnimState.cursePoison);
		DisplayAttackRange(attackArea, 0.35f);
		yield return new WaitForSeconds(0.35f);
		effect.transform.position = targetVec;
		effect.Play();
		if (CheckEnemyInArea(attackArea))
		{
			ApplyBuff(Enemy(), puppet);
			BattleLog("꼭두각시 적용");
			player.Resource++;
		}

		yield return new WaitForSeconds(0.6f);
		SetAnimState(AnimState.idle);
	}
}

public class SpellFireExplosionCommand : Command
{
	public SpellFireExplosionCommand(Direction dir = Direction.right)
		: base(CommandId.SpellFireExplosion, "마법 - 화염 폭발", 2, 1, 60, DirectionType.cross, ClassType.witch)
	{
		this.dir = dir;
		costResource = 2;
		description = string.Format("마력 {0} 필요. 전방의 넓은 범위에 화염 폭발을 일으킵니다.", costResource.ToString());
		previewPos = ((1, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		player.Resource -= costResource;

		List<(int x, int y)> attackArea = new List<(int x, int y)>()
		{ (0,2),(0,1),(0,3),(-1,1),(-1,2),(-1,3),(1,1),(1,2),(1,3) };

		attackArea = CalculateArea(attackArea, player.Pos(), dir);

		var targetVec = GetGrid().PosToVec3(attackArea[0]);

		var effect = GetEffect();
		effect.transform.position = targetVec;

		yield return new WaitForSeconds(0.2f);
		player.tr.LookAt(targetVec);
		SetAnimState(AnimState.spellFireExplosion);
		DisplayAttackRange(attackArea, 0.4f);
		yield return new WaitForSeconds(0.4f);
		effect.Play();
		if (CheckEnemyInArea(attackArea))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);
	}
}

public class SpellLightningCommand : Command
{
	public SpellLightningCommand(Direction dir = Direction.right)
		: base(CommandId.SpellLightning, "마법 - 낙뢰", 2, 1, 120, DirectionType.all, ClassType.witch)
	{
		this.dir = dir;
		costResource = 5;
		description = string.Format("마력 {0} 필요. 전방에 세 번 연속으로 번개를 떨어뜨려 강한 피해를 입힙니다." +
			"모든 방향으로 사용이 가능합니다.", costResource.ToString());
		previewPos = ((0, 2), (3, 2));
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		player.Resource -= costResource;

		List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (0, 2), (0, 3) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		var targetVec = grid.PosToVec3(attackArea[0]);
		player.tr.LookAt(targetVec);

		var effect = GetEffect();

		SetAnimState(AnimState.spellLightning);
		DisplayAttackRange(attackArea, 0.46f);
		yield return new WaitForSeconds(0.46f);
		effect.transform.position = targetVec;
		effect.Play();
		if (CheckEnemyInArea(attackArea[0]))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.35f);
		effect.transform.position = grid.PosToVec3(attackArea[1]);
		effect.Play();
		if (CheckEnemyInArea(attackArea[1]))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.35f);
		effect.transform.position = grid.PosToVec3(attackArea[2]);
		effect.Play();
		if (CheckEnemyInArea(attackArea[2]))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.7f);
		SetAnimState(AnimState.idle);
	}
}

public class EscapeSpellCommand : Command
{
	public EscapeSpellCommand(Direction dir = Direction.right)
		: base(CommandId.EscapeSpell, "탈출 주문", 1, 1, 0, DirectionType.none, ClassType.witch)
	{
		description = "반경 1칸 이내에 적이 있으면 반대 방향으로 2칸 순간이동 합니다. 적이 없다면 20 회복합니다.";
		previewPos = ((2, 2), (3, 2));
		canPredict = false;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		PlayerInfo enemy = GetEnemyInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> checkArea = new List<(int x, int y)>()
		{ (0,1),(1,1),(1,0),(1,-1),(0,-1),(-1,-1),(-1,0),(-1,1) };
		checkArea = CalculateArea(checkArea, player.Pos());
		var effect = GetEffect();
		SetAnimState(AnimState.escapeSpell);

		if (CheckEnemyInArea(checkArea))
		{
			var dirPos = grid.SubtractPos(player.Pos(), enemy.Pos());
			Direction teleDir = Grid.PosToDir(dirPos);

			var telePos = (0, 2);
			telePos = grid.SwitchDir(telePos, teleDir);
			telePos = grid.AddPos(player.Pos(), telePos, true);

			yield return new WaitForSeconds(0.095f);

			player.tr.position = grid.PosToVec3(telePos);
			effect.transform.position = player.tr.position;
			effect.Play();
			player.tr.LookAt(enemy.tr);
			BattleLog("탈출");
		}
		else
		{
			effect.transform.position = player.tr.position;
			effect.Play();
			Restore(20);
		}

		yield return new WaitForSeconds(0.88f);
		SetAnimState(AnimState.idle);
	}
}

#endregion