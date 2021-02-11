using UnityEngine;

public class Demon : ClassSpecialize
{
	public override void Initialize()
	{
		base.Initialize();
	}

	public override void SetBaseStatus(PlayerInfo player)
	{
		player.maxHP = 125 + 25 * (InGame.instance.bossStage - 1);
		player.HP = player.maxHP;
		player.dealDamageBonus = 5 * (InGame.instance.bossStage - 1);
	}

	public override CommandSet GetBossPattern()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		float hpp = bossInfo.HP / (float)bossInfo.maxHP;

		float ran = Random.Range(0, 1f);

		return DarkRedemption();

		if (hpp > 0.5f)
		{
			if (ran < 0.25f)
				return MoveSpinSpinSwing();
			else if (ran < 0.5f)
				return JumpSpinSwingSwing();
			else if (ran < 0.75f)
				return JumpEarthWave();
			else
				return SwingSwingSwing();
		}
		else
		{
			if (ran < 0.5f)
				return MoveSpinIncineration();
			else if (ran < 0.75f)
				return JumpIncineration();
			else
				return InciInci();
		}
	}

	private CommandSet HarvestVanish()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);

		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new VanishCommand());

		return pattern;
	}

	private CommandSet DarkRedemption()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);

		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new DarkRedemption());

		return pattern;
	}

	#region 예시
	// 체력 50퍼 이상

	private CommandSet MoveSpinSpinSwing()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);
		var nextPos = grid.AddPos(bossInfo.Pos(), grid.SwitchDir((0, 2), dir1));
		Direction dir2 = grid.GetSimilarDirection(nextPos, playerInfo.Pos(), DirectionType.cross);

		Command move = new BossMoveSlowCommand(dir1);
		Command spin1 = new SpinSwingCommand(dir1);
		Command spin2 = new SpinSwingCommand(dir2);
		Command swing = new GiantSwingCommand(Grid.OppositeDir(dir2));

		pattern.Push(move);
		pattern.Push(spin1);
		pattern.Push(spin2);
		pattern.Push(swing);

		return pattern;
	}

	private CommandSet JumpSpinSwingSwing()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection((2, 2), playerInfo.Pos(), DirectionType.cross);
		var nextPos = grid.AddPos(bossInfo.Pos(), grid.SwitchDir((0, 1), dir1));
		Direction dir2 = grid.GetSimilarDirection(nextPos, playerInfo.Pos(), DirectionType.all);

		Command jump = new JumpAttackCommand();
		Command spin1 = new SpinSwingCommand(dir1);
		Command swing1 = new GiantSwingCommand(dir2);
		Command swing2 = new GiantSwingCommand(Grid.DirOper(dir2, +1));

		pattern.Push(jump);
		pattern.Push(spin1);
		pattern.Push(swing1);
		pattern.Push(swing2);

		return pattern;
	}

	private CommandSet SwingSwingSwing()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.all);

		Command swing1 = new GiantSwingCommand(dir);
		Command swing2 = new GiantSwingCommand(Grid.DirOper(dir, -1));
		Command swing3 = new GiantSwingCommand(Grid.DirOper(dir, +1));

		pattern.Push(swing1);
		pattern.Push(swing2);
		pattern.Push(swing3);

		return pattern;
	}

	private CommandSet JumpEarthWave()
	{
		CommandSet pattern = new CommandSet();

		Command jump = new JumpAttackCommand();
		Command earthWave = new EarthWaveCommand();

		pattern.Push(jump);
		pattern.Push(earthWave);

		return pattern;
	}

	// 체력 50퍼 이하

	private CommandSet JumpIncineration()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();

		Direction dir = grid.GetSimilarDirection((2, 2), playerInfo.Pos(), DirectionType.cross);

		Command jumpAttack = new JumpAttackCommand();
		Command Incineration = new IncinerationCommand(dir);
		Command swing = new GiantSwingCommand(Grid.OppositeDir(dir));

		pattern.Push(jumpAttack);
		pattern.Push(Incineration);
		pattern.Push(swing);

		return pattern;
	}

	private CommandSet MoveSpinIncineration()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);
		var nextPos = grid.AddPos(bossInfo.Pos(), grid.SwitchDir((0, 2), dir1));
		Direction dir2 = grid.GetSimilarDirection(nextPos, playerInfo.Pos(), DirectionType.cross);

		Command move = new BossMoveSlowCommand(dir1);
		Command spin1 = new SpinSwingCommand(dir1);
		Command inci = new IncinerationCommand(dir2);

		pattern.Push(move);
		pattern.Push(spin1);
		pattern.Push(inci);

		return pattern;
	}

	private CommandSet InciInci()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();

		Direction dir = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);

		Command Incineration1 = new IncinerationCommand(dir);
		Command Incineration2 = new IncinerationCommand(Grid.DirOper(dir, -2));

		pattern.Push(Incineration1);
		pattern.Push(Incineration2);

		return pattern;
	}
}

#endregion