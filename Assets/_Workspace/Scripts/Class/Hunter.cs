using UnityEngine;

public class Hunter : ClassSpecialize
{
	public override void Initialize()
	{
		base.Initialize();
	}

	public override void SetBaseStatus(PlayerInfo player)
	{
		player.maxHP = 150;
		player.HP = 150;
		player.resourceClamp = (0, 0);
		player.Resource = 0;
	}
}
