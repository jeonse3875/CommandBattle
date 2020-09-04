using UnityEngine;

public class GiantGolem : ClassSpecialize
{
	public override void Initialize()
	{
		base.Initialize();
	}

	public override void SetBaseStatus(PlayerInfo player)
	{
		player.maxHP = 50 + 50 * InGame.instance.bossStage;
		player.HP = player.maxHP;
		player.dealDamageBonus = 3 * InGame.instance.bossStage;
	}
}
