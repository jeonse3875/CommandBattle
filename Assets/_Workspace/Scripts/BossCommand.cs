using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossType
{
	common, mechGolem, demon
}

public enum BossCommandId
{
	Empty, BossMoveSlow, GiantSwing, Incineration, JumpAttack, SpinSwing, Harvest, DarkRedemption
}

public class BossEmptyCommand : Command
{
	public BossEmptyCommand(Direction dir = Direction.right)
		: base(BossCommandId.Empty, "Empty", 1, 0, DirectionType.none, BossType.common)
	{

	}

	public override IEnumerator Execute()
	{
		AnimState lastState = GetCommanderInfo().lastAnimState;
		if (lastState.Equals(AnimState.bossRun) || lastState.Equals(AnimState.stiff))
			SetAnimState(AnimState.idle);
		yield break;
	}
}

public class BossMoveSlowCommand : Command
{
	public BossMoveSlowCommand(Direction dir = Direction.right) : base(BossCommandId.BossMoveSlow, "이동", 2, 0, DirectionType.all, BossType.common)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();

		var targetPos = (0, 1);
		targetPos = CalculateArea(targetPos, player.Pos(), dir);
		targetPos = grid.ClampPos(targetPos);
		var targetVec = grid.PosToVec3(targetPos);

		player.tr.LookAt(targetVec);
		SetAnimState(AnimState.bossRun);
		movingTween = player.tr.DOMove(targetVec, time).SetEase(Ease.Linear);
		BattleLog(string.Format("{0} 이동", GGrid.DirToKorean(dir)));
		yield return new WaitForSeconds(1.95f);
	}
}

#region MechGolem

public class GiantSwingCommand : Command
{
	public GiantSwingCommand(Direction dir = Direction.right) : base(BossCommandId.GiantSwing, "휘두르기", 3, 50, DirectionType.all, BossType.common)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (0, 2), (0, 3) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		var targetVec = grid.PosToVec3(attackArea[0]);
		player.tr.LookAt(targetVec);

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		var effect1 = GetEffect(1);
		var effect2 = GetEffect(2);
		var effect3 = GetEffect(3);

		SetAnimState(AnimState.giantSwing);
		yield return new WaitForSeconds(0.3f);
		DisplayAttackRange(attackArea, 1f);
		yield return new WaitForSeconds(1f);
		effect1.transform.position = grid.PosToVec3(attackArea[0]);
		effect1.Play();
		if (CheckEnemyInArea(attackArea[0]))
		{
			Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(0.2f);
		effect2.transform.position = grid.PosToVec3(attackArea[1]);
		effect2.Play();
		if (CheckEnemyInArea(attackArea[1]))
		{
			Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(0.2f);
		effect3.transform.position = grid.PosToVec3(attackArea[2]);
		effect3.Play();
		if (CheckEnemyInArea(attackArea[2]))
		{
			Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(0.6f);
		SetAnimState(AnimState.idle);
	}
}

public class IncinerationCommand : Command
{
	public IncinerationCommand(Direction dir = Direction.right) : base(BossCommandId.Incineration, "소각", 5, 10, DirectionType.cross, BossType.mechGolem)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)>()
		{ (0, 1), (0, 2), (-1, 1), (-1, 2), (1, 1), (1, 2), };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		var targetVec = grid.PosToVec3(attackArea[0]);
		player.tr.LookAt(targetVec);
		var effect = GetEffect();
		effect.transform.position = grid.PosToVec3(player.Pos());
		effect.transform.LookAt(targetVec);
		Vector3 origin = effect.transform.eulerAngles;
		effect.transform.eulerAngles += new Vector3(0, -35f, 0);

		SetAnimState(AnimState.incineration);
		yield return new WaitForSeconds(0.55f);
		DisplayAttackRange(attackArea, 1f);
		yield return new WaitForSeconds(1f);
		BattleLog("주기적인 피해");
		effect.Play();
		DOTween.Sequence()
			.Append(effect.transform.DORotate(origin + new Vector3(0, 35f, 0), 1.25f))
			.Append(effect.transform.DORotate(origin + new Vector3(0, -20f, 0), 1.25f))
			.Append(effect.transform.DORotate(origin + Vector3.zero, 0.5f))
			.OnComplete(() => { effect.Stop(); });

		float progress = 0f;
		var wait = new WaitForSeconds(0.3f);
		while (progress < 3.1f)
		{
			yield return wait;
			if (CheckEnemyInArea(attackArea))
				Hit(totalDamage, true, false);
			progress += 0.3f;
		}
		SetAnimState(AnimState.idle);
	}
}

public class JumpAttackCommand : Command
{
	public JumpAttackCommand(Direction dir = Direction.right) : base(BossCommandId.JumpAttack, "내려찍기", 2, 60, DirectionType.none, BossType.common)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();

		Buff unStop = new Buff(BuffCategory.unStoppable, true);
		unStop.SetDuration(3f);
		ApplyBuff(commander, unStop);

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		var effect = GetEffect();

		List<(int x, int y)> attackArea = new List<(int x, int y)>()
		{ (0,0),(0,-1),(0,1),(-1,0),(-1,-1),(-1,1),(1,0),(1,-1),(1,1)};
		attackArea = CalculateArea(attackArea, (2, 2));
		var targetVec = grid.PosToVec3(attackArea[0]);
		player.tr.LookAt(targetVec);
		SetAnimState(AnimState.jumpAttack);
		yield return new WaitForSeconds(0.33f);
		BattleLog("뛰어오름");
		DisplayAttackRange(attackArea, 0.73f);
		movingTween = player.tr.DOMove(targetVec, 0.73f).SetEase(Ease.Unset);
		yield return new WaitForSeconds(0.73f);
		effect.transform.position = targetVec;
		effect.Play();
		if (CheckEnemyInArea(attackArea))
		{
			Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(0.65f);
		SetAnimState(AnimState.idle);
	}
}

public class SpinSwingCommand : Command
{
	public  SpinSwingCommand(Direction dir = Direction.right) : base(BossCommandId.SpinSwing, "휩쓸기", 2, 30, DirectionType.cross, BossType.common)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();

		List<(int x, int y)> attackArea1 = new List<(int x, int y)>() { (-1, 0), (1, 0), (0, -1) };
		List<(int x, int y)> attackArea2 = new List<(int x, int y)>() { (0, 2), (-1, 1), (1, 1) };
		attackArea1 = CalculateArea(attackArea1, player.Pos(), dir);
		attackArea2 = CalculateArea(attackArea2, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea2[0]));
		var targetVec = grid.PosToVec3(grid.ClampPos(CalculateArea((0, 1), player.Pos(), dir)));

		SetAnimState(AnimState.spinSwing);
		DisplayAttackRange(attackArea1, 0.56f);
		DisplayAttackRange(attackArea2, 0.83f);
		yield return new WaitForSeconds(0.23f);
		movingTween = player.tr.DOMove(targetVec, 0.94f);
		yield return new WaitForSeconds(0.33f);
		if(CheckEnemyInArea(attackArea1))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.27f);
		if (CheckEnemyInArea(attackArea2))
		{
			Hit(totalDamage);
		}
		yield return new WaitForSeconds(0.73f);
		SetAnimState(AnimState.idle);
	}
}

#endregion

#region Demon

public class HarvestCommand : Command
{
	public HarvestCommand(Direction dir = Direction.right)
		: base(BossCommandId.Harvest, "수확", 2, 30, DirectionType.cross, BossType.demon)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();
		var effect = GetEffect();
		Transform enemyTr = GetEnemyInfo().tr;

		List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (-1, 1), (1, 1) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.harvest);
		DisplayAttackRange(attackArea, 0.66f);
		yield return new WaitForSeconds(0.66f);
		if (CheckEnemyInArea(attackArea))
		{
			int realDamage = Hit(damage);
			player.Restore(realDamage);
			effect.transform.position = enemyTr.position;
			effect.Play();
		}
		yield return new WaitForSeconds(0.6f);
		SetAnimState(AnimState.idle);

		yield break;
	}
}

public class DarkRedemption : Command
{
	public DarkRedemption(Direction dir = Direction.right)
		: base(BossCommandId.DarkRedemption, "악의 구원", 7, 5, DirectionType.none, BossType.demon)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		GGrid grid = GetGrid();
		var effect = GetEffect();
		Transform enemyTr = GetEnemyInfo().tr;

		List<(int x, int y)> attackArea = new List<(int x, int y)>()
		{ (0, -1), (-1, 1), (0, 1), (1, 1), (-1, 0), (1, 0), (-1, -1), (0, 0), (1, -1) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));
		effect.transform.position = grid.PosToVec3(player.Pos());

		// 수치조정
		int damage = this.totalDamage;
		SetAnimState(AnimState.darkRedemption);
		player.specialize.HideWeapon(5.85f);
		DisplayAttackRange(attackArea, 0.4f);
		yield return new WaitForSeconds(0.35f);
		BattleLog("주기적인 흡혈");
		float progress = 0f;
		effect.Play();
		var wait = new WaitForSeconds(0.3f);
		while (progress < 5.2f)
		{
			yield return wait;
			if (CheckEnemyInArea(attackArea))
			{
				int realDamage = Hit(totalDamage, true, false);
				Restore(realDamage, false);
			}
			progress += 0.3f;
		}

		yield return new WaitForSeconds(0.4f);
		SetAnimState(AnimState.idle);
		

		yield break;
	}
}

#endregion