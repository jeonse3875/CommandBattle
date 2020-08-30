using UnityEngine;

public class Knight : ClassSpecialize
{
	public override void Initialize()
	{
		base.Initialize();
	}

	public override void SetBaseStatus(PlayerInfo player)
	{
		player.maxHP = 250;
		player.HP = 250;
		player.resourceClamp = (0, 0);
		player.Resource = 0;
	}

	public override Buff[] GetPassive()
	{
		Buff takeDamageReduce = new Buff(BuffCategory.takeDamage, true, -0.1f);

		return new Buff[] { takeDamageReduce };
	}
}
