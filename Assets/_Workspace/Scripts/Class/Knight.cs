using System.Collections;
using System.Collections.Generic;
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
		player.hp = 250;
		player.resourceClamp = (0, 0);
		player.Resource = 0;
	}
}
