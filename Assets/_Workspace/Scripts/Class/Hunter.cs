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
		player.resourceClamp = (0, 20);
		player.Resource = 0;
	}

	public override Buff[] GetPassive() // 공격이 빗나갈 때마다 다음 공격에 5의 추가 피해 (최대 20)
	{
		Buff gainResourceByMiss = new Buff(BuffCategory.gainResourceByMiss, true, 5);
		Buff gainResourceByHit = new Buff(BuffCategory.gainResourceByHit, false, -20);
		Buff resourceToBonusDamage = new Buff(BuffCategory.resourceToBonusDamage, true);

		return new Buff[] { gainResourceByMiss, gainResourceByHit, resourceToBonusDamage };
	}
}
