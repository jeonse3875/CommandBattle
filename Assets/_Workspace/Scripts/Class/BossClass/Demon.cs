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

	// 패턴
	// 1페이즈: 수확, 전투준비, 은신, 경직
	// 2페이즈: 악의구원
	public override CommandSet GetBossPattern()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		float hpp = bossInfo.HP / (float)bossInfo.maxHP;

		float ran = Random.Range(0, 1f);

		if (hpp > 0.6f)
		{
			if (ran < 0.4f)
				return CombatReadyMoveHarvestX2Vanish();
			else if (ran < 0.8f)
				return MoveMoveStiffHarvestHarvestVanish();
			else
				return RandomHarvest();
		}
		else
		{
			if (ran < 0.5f)
				return DarkRedemptionVanishMoveMove();
			else
				return MoveHarvestDarkRedemption();
		}
	}

	private CommandSet CombatReadyMoveHarvestX2Vanish()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);
		Direction dir2 = GGrid.RandomDir(DirectionType.cross);
		Direction dir3 = GGrid.RandomDir(DirectionType.cross);

		pattern.Push(new CombatReadyCommand());
		pattern.Push(new MoveCommand(dir1));
		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new MoveCommand(dir2));
		pattern.Push(new HarvestCommand(dir2));
		pattern.Push(new VanishCommand());
		pattern.Push(new MoveCommand(dir3));

		return pattern;
	}

	private CommandSet MoveMoveStiffHarvestHarvestVanish()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);
		Direction dir2 = GGrid.RandomDir(DirectionType.cross);
		Direction dir3 = GGrid.RandomDir(DirectionType.cross);

		pattern.Push(new MoveCommand(dir1));
		pattern.Push(new MoveCommand(dir1));
		pattern.Push(new CurseStiffCommand());
		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new HarvestCommand(dir2));
		pattern.Push(new VanishCommand());
		pattern.Push(new MoveCommand(dir3));
		pattern.Push(new MoveCommand(dir3));

		return pattern;
	}

	private CommandSet RandomHarvest()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);
		Direction dir2 = GGrid.RandomDir(DirectionType.cross);
		Direction dir3 = GGrid.RandomDir(DirectionType.cross);


		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new HarvestCommand(dir2));
		pattern.Push(new MoveCommand(dir3));
		pattern.Push(new HarvestCommand(dir3));
		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new VanishCommand());

		return pattern;
	}

	private CommandSet MoveHarvestDarkRedemption()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);

		pattern.Push(new MoveCommand(dir1));
		pattern.Push(new HarvestCommand(dir1));
		pattern.Push(new DarkRedemption());

		return pattern;
	}

	private CommandSet DarkRedemptionVanishMoveMove()
	{
		var bossInfo = InGame.instance.playerInfo[Who.p2];
		var playerInfo = InGame.instance.playerInfo[Who.p1];
		var grid = InGame.instance.grid;

		CommandSet pattern = new CommandSet();
		Direction dir1 = grid.GetSimilarDirection(bossInfo.Pos(), playerInfo.Pos(), DirectionType.cross);
		Direction dir2 = GGrid.RandomDir(DirectionType.cross);

		pattern.Push(new DarkRedemption());
		pattern.Push(new VanishCommand());
		pattern.Push(new MoveCommand(dir2));
		pattern.Push(new MoveCommand(dir2));

		return pattern;
	}
}