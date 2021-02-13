using UnityEngine;

public class Pirate : ClassSpecialize
{
	public override void Initialize()
	{
		base.Initialize();
	}

	public override void SetBaseStatus(PlayerInfo player)
	{
		player.maxHP = 200;
		player.HP = 200;
		player.resourceClamp = (0, 3);
		player.Resource = 0;
	}

	public override Buff[] GetPassive()
	{
		Buff gainResource1 = new Buff(BuffCategory.gainResourceByDealDamage, true, 1);

		return new Buff[] { gainResource1 };
	}
}
