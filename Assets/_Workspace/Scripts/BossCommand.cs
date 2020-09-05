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
	Empty, BossMove, GiantSwing,
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
		if (lastState.Equals(AnimState.run) || lastState.Equals(AnimState.stiff))
			SetAnimState(AnimState.idle);
		yield break;
	}
}

public class BossMoveCommand : Command
{
	public BossMoveCommand(Direction dir) : base(BossCommandId.BossMove, "이동", 2, 0, DirectionType.cross, BossType.common)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		var targetPos = (0, 1);
		targetPos = CalculateArea(targetPos, player.Pos(), dir);
		targetPos = grid.ClampPos(targetPos);
		var targetVec = grid.PosToVec3(targetPos);

		SetAnimState(AnimState.bossRun);
		movingTween = player.tr.DOMove(targetVec, time).SetEase(Ease.Linear);
		BattleLog(string.Format("{0} 이동", Grid.DirToKorean(dir)));
		yield break;
	}
}

public class GiantSwingCommand : Command
{
	public GiantSwingCommand(Direction dir) : base(BossCommandId.GiantSwing, "휘두르기", 3, 50, DirectionType.cross, BossType.common)
	{
		this.dir = dir;
	}

	public override IEnumerator Execute()
	{
		PlayerInfo player = GetCommanderInfo();
		Grid grid = GetGrid();

		List<(int x, int y)> attackArea = new List<(int x, int y)>() { (0, 1), (0, 2) };
		attackArea = CalculateArea(attackArea, player.Pos(), dir);
		player.tr.LookAt(grid.PosToVec3(attackArea[0]));

		Buff stiff = new Buff(BuffCategory.stiff, false);
		stiff.SetCount(CountType.instant);

		SetAnimState(AnimState.giantSwing);
		yield return new WaitForSeconds(0.3f);
		DisplayAttackRange(attackArea, 1f);
		yield return new WaitForSeconds(1f);
		if (CheckEnemyInArea(attackArea))
		{
			Hit(totalDamage, stiff);
		}
		yield return new WaitForSeconds(1f);
		SetAnimState(AnimState.idle);
	}
}