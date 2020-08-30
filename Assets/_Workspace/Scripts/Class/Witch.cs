using UnityEngine;

public class Witch : ClassSpecialize
{
	public override void Initialize()
	{
		base.Initialize();
	}

	public override void SetBaseStatus(PlayerInfo player)
	{
		player.maxHP = 150;
		player.HP = 150;
		player.resourceClamp = (0, 5);
		player.Resource = 0;
	}
}
