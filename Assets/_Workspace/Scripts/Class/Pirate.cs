using UnityEngine;

public class Pirate : ClassSpecialize
{
	public int crewCount = 0;
	public int crew_deckhand = 0;
	public int crew_medical = 0;
	public int crew_tombraider = 0;
	

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

	public void HireDeckHand(Who who)
	{
		if (crew_deckhand >= 3)
			return;
		InGame.instance.SummonDeckhand(who, crew_deckhand);
		crew_deckhand++;
		crewCount++;
	}

	public void HireMedical(Who who)
	{
		if (crew_medical >= 3)
			return;
		InGame.instance.SummonMedical(who, crew_medical);
		crew_medical++;
		crewCount++;
	}

	public void HireTombraider(Who who)
	{
		if (crew_tombraider >= 3)
			return;
		InGame.instance.SummonTombraider(who, crew_tombraider);
		crew_tombraider++;
		crewCount++;
	}
}
