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
			else
				buff.leftDuration -= passedTime;

			if (buff.CheckBuffEnd())
				buff.Release(player);
		}
		buffList.RemoveAll((Buff buff) => { return buff.isEnd; });
	}

	public void UpdateCount(CountType countType, int change)
	{
		foreach (var buff in buffList)
		{
			if (buff.buffType.Equals(BuffType.count) && buff.countType.Equals(countType))
			{
				buff.leftCount += change;
			}
		}
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
	public BuffType buffType;
	public bool isApplied = false;
	public bool isGood;
	public float amount;

	public bool isEnd = false;

	public float leftDuration;

	public CountType countType;
	public int leftCount;

	private GameObject effectObj;

	public Buff(BuffCategory category, bool isGood, float amount = 0f)
	{
		this.category = category;
		this.isGood = isGood;
		this.amount = amount + 1;
	}

	public void SetDuration(float duration)
	{
		this.buffType = BuffType.duration;
		this.leftDuration = duration;
	}

	public void SetCount(CountType countType, int count = 1)
	{
		this.buffType = BuffType.count;
		this.countType = countType;
		this.leftCount = count;
	}

	public void Apply(PlayerInfo player)
	{
		if (isApplied)
			return;
		isApplied = true;

		effectObj = InGame.instance.InstantiateBuffEffect(this);

		switch (category)
		{
			case BuffCategory.takeDamage:
				player.takeDamageMultiplier *= amount;
				if (amount < 1f)
				{
					effectObj.transform.position = player.tr.position;
					effectObj.GetComponent<FollowPlayer>().target = player.tr;
				}
				break;
			case BuffCategory.dealDamage:
				player.dealDamageMultiplier *= amount;
				if (amount > 1f)
				{
					effectObj.transform.position = player.tr.position;
					effectObj.GetComponent<FollowPlayer>().target = player.tr;
				}
				break;
			case BuffCategory.stiff:
				InGame.instance.StopCommand(player.me);
				player.SetAnimState(AnimState.stiff);
				break;
			default:
				break;
		}

		if (buffType.Equals(BuffType.count) && countType.Equals(CountType.instant))
			leftCount--;
	}

	public bool CheckBuffEnd()
	{
		if (!isApplied)
			return false;

		if (buffType.Equals(BuffType.duration))
		{
			if (leftDuration > 0)
				return false;
		}
		else if (buffType.Equals(BuffType.count))
		{
			if (leftCount > 0)
				return false;
		}

		return true;
	}

	public void Release(PlayerInfo player)
	{
		switch (category)
		{
			case BuffCategory.takeDamage:
				player.takeDamageMultiplier /= amount;
				break;
			case BuffCategory.dealDamage:
				player.dealDamageMultiplier /= amount;
				break;
			case BuffCategory.stiff:
				break;
			default:
				break;
		}

		isEnd = true;
		if (effectObj != null)
			InGame.instance.DestroyObj(effectObj);
	}
}

public enum BuffCategory
{
	takeDamage, dealDamage, stiff
}

public enum BuffType
{
	duration, count
}

public enum CountType
{
	instant, getHit, hit
}