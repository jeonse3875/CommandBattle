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

	public BuffSet(PlayerInfo player)
	{
		this.who = player.me;
		this.player = player;
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
			if (buff.buffType.Equals(BuffType.count) && buff.countType.Contains(countType))
			{
				buff.leftCount += change;
			}
		}
	}

	public void Clear(bool exceptMultiTurn = false)
	{
		List<Buff> removeList = new List<Buff>();

		foreach (var buff in buffList)
		{
			if (exceptMultiTurn && buff.isMultiTurn)
				continue;

			if (buff.isPassive)
				continue;
			
			if (buff.isApplied)
			{
				buff.Release(player);
				removeList.Add(buff);
			}
		}

		foreach(var buff in removeList)
		{
			buffList.Remove(buff);
		}
	}
}

public class Buff
{
	public bool isPreview = false;
	public bool isPassive = false;

	public BuffCategory category;
	public BuffType buffType;
	public bool isPercentage;
	public bool isApplied = false;
	public bool isGood;
	public float amount_Percentage;
	public int amount_Int;

	public bool isEnd = false;

	public float leftDuration;

	public List<CountType> countType;
	public int leftCount;

	public bool isMultiTurn = false;

	private bool hasChainBuff = false;
	private Buff chainBuff;

	private GameObject effectObj;

	public Buff(BuffCategory category, bool isGood, float amount = 0f)
	{
		isPercentage = true;
		this.category = category;
		this.isGood = isGood;
		this.amount_Percentage = amount + 1;
	}

	public Buff(BuffCategory category, bool isGood, int amount)
	{
		isPercentage = false;
		this.category = category;
		this.isGood = isGood;
		this.amount_Int = amount;
	}

	public void SetPassive()
	{
		this.isPassive = true;
	}

	public void SetDuration(float duration)
	{
		this.buffType = BuffType.duration;
		this.leftDuration = duration;
	}

	public void SetCount(CountType countType, int count = 1)
	{
		this.buffType = BuffType.count;
		this.countType = new List<CountType>() { countType };
		this.leftCount = count;
	}

	public void SetCount(List<CountType> countType, int count = 1)
	{
		this.buffType = BuffType.count;
		this.countType = countType;
		this.leftCount = count;
	}

	public void SetChainBuff(Buff chainBuff)
	{
		this.chainBuff = chainBuff;
		this.hasChainBuff = true;
	}

	public void Apply(PlayerInfo player)
	{
		if (isApplied)
			return;
		isApplied = true;

		effectObj = InGame.InstantiateBuffEffect(this);

		switch (category)
		{
			case BuffCategory.takeDamage:
				if (isPercentage)
				{
					player.takeDamageMultiplier *= amount_Percentage;
					effectObj.transform.position = player.tr.position;
					effectObj.GetComponent<FollowPlayer>().target = player.tr;
				}
				else
				{
					
				}
				
				break;
			case BuffCategory.dealDamage:
				if (isPercentage)
				{
					player.dealDamageMultiplier *= amount_Percentage;
					if (amount_Percentage > 1f)
					{
						effectObj.transform.position = player.tr.position;
						effectObj.GetComponent<FollowPlayer>().target = player.tr;
					}
				}
				else
				{

				}
				break;
			case BuffCategory.stiff:
				if (!isPreview)
				{
					if (InGame.instance.StopCommand(player.me))
						player.SetAnimState(AnimState.stiff);
				}
				else
					player.SetAnimState(AnimState.stiff);
				break;
			case BuffCategory.gainResourceByDealDamage:
				player.resourceByDealDamage += amount_Int;
				break;
			case BuffCategory.gainResourceByTakeDamage:
				player.resourceByTakeDamage += amount_Int;
				break;
			case BuffCategory.unStoppable:
				player.isUnstoppable = true;
				break;
			case BuffCategory.paralysis:
				player.isParalysis = true;
				break;
			case BuffCategory.vanish:
				player.isVanish = true;
				player.specialize.Vanish();
				if (isPreview)
				{
					effectObj.transform.position = player.tr.position;
					effectObj.GetComponent<FollowPlayer>().target = player.tr;
				}
				else
				{
					if (InGame.instance.me.Equals(player.me))
					{
						effectObj.transform.position = player.tr.position;
						effectObj.GetComponent<FollowPlayer>().target = player.tr;
					}
					else
					{
						effectObj.SetActive(false);
						InGame.instance.particles[player.me].gameObject.SetActive(false);
						foreach(var effect in InGame.instance.effectObj[player.me])
						{
							if (effect != null)
								effect.SetActive(false);
						}
					}
				}
				break;
			case BuffCategory.gainResourceByMiss:
				player.resourceByMiss += amount_Int;
				break;
			case BuffCategory.gainResourceByHit:
				player.resourceByHit += amount_Int;
				break;
			case BuffCategory.resourceToBonusDamage:
				player.canResourceToBonusDamage = true;
				break;
			case BuffCategory.poison:
				player.poisonCount += amount_Int;
				effectObj.transform.position = player.tr.position;
				effectObj.GetComponent<FollowPlayer>().target = player.tr;
				break;
			default:
				break;
		}

		if (player.isVanish && !category.Equals(BuffCategory.vanish))
			effectObj.SetActive(false);

		if (isPassive)
			InGame.DestroyObj(effectObj);

		if (effectObj != null && !isPreview)
		{
			InGame.instance.effectObj[player.me].Add(effectObj);
		}

		if (buffType.Equals(BuffType.count) && countType.Equals(CountType.instant))
			leftCount--;
	}

	public bool CheckBuffEnd()
	{
		if (!isApplied)
			return false;

		if (isPassive)
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
				player.takeDamageMultiplier /= amount_Percentage;
				break;
			case BuffCategory.dealDamage:
				player.dealDamageMultiplier /= amount_Percentage;
				break;
			case BuffCategory.stiff:
				break;
			case BuffCategory.gainResourceByDealDamage:
				player.resourceByDealDamage -= amount_Int;
				break;
			case BuffCategory.gainResourceByTakeDamage:
				player.resourceByTakeDamage -= amount_Int;
				break;
			case BuffCategory.unStoppable:
				player.isUnstoppable = true;
				break;
			case BuffCategory.paralysis:
				player.isParalysis = false;
				break;
			case BuffCategory.vanish:
				player.isVanish = false;
				player.specialize.UnVanish();
				if (!isPreview)
				{
					if (InGame.instance.me.Equals(player.me))
					{

					}
					else
					{
						InGame.instance.particles[player.me].gameObject.SetActive(true);
						foreach (var effect in InGame.instance.effectObj[player.me])
						{
							if (effect != null)
								effect.SetActive(true);
						}
					}
				}
				break;
			case BuffCategory.gainResourceByMiss:
				player.resourceByMiss -= amount_Int;
				break;
			case BuffCategory.gainResourceByHit:
				player.resourceByHit -= amount_Int;
				break;
			case BuffCategory.resourceToBonusDamage:
				player.canResourceToBonusDamage = false;
				break;
			case BuffCategory.poison:
				player.poisonCount -= amount_Int;
				break;
			default:
				break;
		}

		isEnd = true;

		if (effectObj != null)
		{
			if (!isPreview)
				InGame.instance.effectObj[player.me].Remove(effectObj);
			InGame.DestroyObj(effectObj);
		}

		if (hasChainBuff && chainBuff != null)
		{
			if (!isPreview)
			{
				InGame.instance.chainBuffList[player.me].Add(chainBuff);
			}
		}
	}
}

public enum BuffCategory
{
	takeDamage, dealDamage, stiff, gainResourceByTakeDamage, gainResourceByDealDamage,
	unStoppable, paralysis, vanish, gainResourceByMiss, gainResourceByHit, resourceToBonusDamage,
	poison,
}

public enum BuffType
{
	duration, count
}

public enum CountType
{
	instant, takeDamage, dealDamage, tryAttack, permanent
}