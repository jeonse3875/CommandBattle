using System.Collections.Generic;
using UnityEngine;

public class BuffSet
{
	public Who who;
	public PlayerInfo player;
	public List<Buff> buffList = new List<Buff>();

	public BuffSet(Who who)
	{
		this.who = who;
		this.player = InGame.instance.playerInfo[who];
	}

	public void Add(Buff buff)
	{
		buffList.Add(buff);
	}

	public void Update(float passedTime)
	{
		foreach (var buff in buffList)
		{
			if (!buff.isApplied)
				buff.Apply(player);

			buff.leftDuration -= passedTime;

			if (buff.leftDuration < 0)
				buff.Release(player);
		}
		buffList.RemoveAll((Buff buff) => { return buff.leftDuration < 0; });
	}

	public void Clear()
	{
		foreach (var buff in buffList)
		{
			buff.Release(player);
		}
		buffList.Clear();
	}

	public void DeactivateBuff(bool isGood)
	{
		buffList.RemoveAll((Buff buff) => { return buff.isGood.Equals(isGood); });
	}
}

public class Buff
{
	public BuffCategory category;
	public bool isApplied = false;
	public bool isGood;
	public float amount;
	public float duration;
	public float leftDuration;

	private GameObject effectObj;

	public Buff(BuffCategory category, bool isGood, float duration, float amount = 0f)
	{
		this.category = category;
		this.isGood = isGood;
		this.amount = amount + 1;
		this.duration = duration;
		this.leftDuration = duration;
	}

	public void Apply(PlayerInfo player)
	{
		if (isApplied)
			return;
		isApplied = true;

		switch (category)
		{
			case BuffCategory.takenDamage:
				player.takenDamageMultiplier *= amount;
				if (amount < 1f)
				{
					effectObj = InGame.instance.InstantiateBuffEffect("DamageReduce");
					effectObj.transform.position = player.tr.position;
				}
				break;
			case BuffCategory.stiff:
				InGame.instance.StopCommand(player.me);
				player.SetAnimState(AnimState.stiff);
				break;
			default:
				break;
		}
	}

	public void Release(PlayerInfo player)
	{
		if (!isApplied)
			return;

		switch (category)
		{
			case BuffCategory.takenDamage:
				player.takenDamageMultiplier /= amount;
				if (effectObj != null)
					InGame.instance.DestroyObj(effectObj);
				break;
			case BuffCategory.stiff:
				if (player.lastAnimState.Equals(AnimState.stiff))
					player.SetAnimState(AnimState.idle);
				break;
			default:
				break;
		}
	}
}

public enum BuffCategory
{
	takenDamage, stiff
}
