using UnityEngine;

public enum MapEvent
{
	none = 0, heal, damageBonus,
}

public enum BossMapEvent
{
	none = 0, heal, damageBonus, maxHP,
}

public class MapEventPickUp : MonoBehaviour
{
	public static int curPickUpCount = 0;
	public static int totalPickUpCount = 0;

	private bool isBossRush = false;

	private MapEvent mapEvent;
	private BossMapEvent bossMapEvent;
	private (int x, int y) pos;

	private void Start()
	{
		curPickUpCount++;
		totalPickUpCount++;
	}

	private void OnDestroy()
	{
		curPickUpCount--;
	}

	private void Update()
	{
		if (InGame.instance.playerInfo[Who.p1].Pos().Equals(pos))
		{
			if (isBossRush)
				Apply_Boss(InGame.instance.playerInfo[Who.p1]);
			else
				Apply(InGame.instance.playerInfo[Who.p1]);
		}

		if (InGame.instance.playerInfo[Who.p2].Pos().Equals(pos))
		{
			if (isBossRush)
				Apply_Boss(InGame.instance.playerInfo[Who.p2]);
			else
				Apply(InGame.instance.playerInfo[Who.p2]);
		}
	}

	public void SetPickUp(MapEvent mapEvent, (int x, int y) pos)
	{
		this.mapEvent = mapEvent;
		this.pos = pos;
	}

	public void SetPickUp(BossMapEvent mapEvent, (int x, int y) pos)
	{
		isBossRush = true;
		this.bossMapEvent = mapEvent;
		this.pos = pos;
	}

	private void Apply(PlayerInfo player)
	{
		switch (mapEvent)
		{
			case MapEvent.heal:
				player.Restore(20);
				break;
			case MapEvent.damageBonus:
				player.dealDamageBonus += 10;
				InGame.instance.InstantiateDamageTMP(player.tr, "데미지 증가", 0);
				break;
			default:
				break;
		}

		Destroy(this.gameObject);
	}

	private void Apply_Boss(PlayerInfo player)
	{
		switch (bossMapEvent)
		{
			case BossMapEvent.heal:
				player.Restore(Mathf.CeilToInt(player.maxHP * 0.1f));
				break;
			case BossMapEvent.damageBonus:
				player.dealDamageBonus += 10;
				InGame.instance.InstantiateDamageTMP(player.tr, "데미지 증가", 0);
				break;
			case BossMapEvent.maxHP:
				player.maxHP += 15;
				player.HP += 15;
				InGame.instance.InstantiateDamageTMP(player.tr, "최대 체력 증가", 0);
				InGame.instance.inGameUI.UpdateHealth(player.me);
				break;
			default:
				break;
		}

		Destroy(this.gameObject);
	}
}
