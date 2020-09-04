using UnityEngine;

public class MapEventPickUp : MonoBehaviour
{
	public static int curPickUpCount = 0;
	public static int totalPickUpCount = 0;

	private MapEvent mapEvent;
	private (int x, int y) pos;
	private PlayerInfo p1;
	private PlayerInfo p2;

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
		if (p1.Pos().Equals(pos))
		{
			Apply(p1);
		}

		if (p2.Pos().Equals(pos))
		{
			Apply(p2);
		}
	}

	public void SetPickUp(MapEvent mapEvent, (int x, int y) pos)
	{
		this.mapEvent = mapEvent;
		this.pos = pos;

		p1 = InGame.instance.playerInfo[Who.p1];
		p2 = InGame.instance.playerInfo[Who.p2];
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
}
