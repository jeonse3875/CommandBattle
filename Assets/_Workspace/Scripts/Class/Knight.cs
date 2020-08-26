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
}
