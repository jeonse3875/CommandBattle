using UnityEngine;

public class PlayerInfo
{
	public bool isPreview;
	public LobbyUI lobby;
	public ClassSpecialize specialize;

	public Who me;
	public Who enemy;
	public int x;
	public int y;
	public int maxHP;
	private int hp;
	public int HP
	{
		get { return hp; }
		set { hp = Mathf.Clamp(value, 0, maxHP); }
	}
	public (int min, int max) resourceClamp;
	private int resource;

	public int Resource
	{
		get { return resource; }
		set { resource = Mathf.Clamp(value, resourceClamp.min, resourceClamp.max); }
	}

	public float takeDamageMultiplier = 1f;
	public int takeDamageBonus = 0;
	public float dealDamageMultiplier = 1f;
	public int dealDamageBonus = 0;
	public int resourceByDealDamage = 0;
	public int resourceByTakeDamage = 0;
	public int resourceByMiss = 0;
	public int resourceByHit = 0;

	public Transform tr;
	public Animator animator;
	public AnimState lastAnimState;
	public bool isDead = false;
	public int transformCount = 0;
	public bool isUnstoppable = false;
	public bool isParalysis = false;
	public bool isVanish = false;
	public bool isPuppet = false;
	public bool canResourceToBonusDamage = false;
	public int poisonCount = 0;
	public bool isThornArmor = false;

	public PlayerInfo(Who who, (int x, int y) pos, Transform tr)
	{
		x = pos.x;
		y = pos.y;
		this.tr = tr;
		this.me = who;

		if (me.Equals(Who.p1))
			enemy = Who.p2;
		else
			enemy = Who.p1;

		Grid grid = new Grid(5, 5, 5f, new Vector3(-10, 0, -10));
		tr.position = grid.PosToVec3(Pos());
		animator = tr.GetComponent<Animator>();

		InitializeByClass();
		if(specialize != null)
		{
			specialize.SetBaseStatus(this);
		}
	}

	private void InitializeByClass()
	{
		specialize = tr.GetComponent<ClassSpecialize>();
		if (specialize == null)
			return;

		specialize.Initialize();
	}

	public (int x, int y) Pos()
	{
		return (x, y);
	}

	public void SetPos((int x, int y) pos)
	{
		x = pos.x;
		y = pos.y;
	}

	public int TakeDamage(int damage, int originDamage, bool isMultiple = false)
	{
		if (isDead)
			return 0;

		int takenDamage = Mathf.RoundToInt(damage * takeDamageMultiplier) + takeDamageBonus;
		int reducedDamage = Mathf.Abs(damage - takenDamage);
		int mode;

		if (takenDamage > originDamage)
			mode = 1;
		else if (takenDamage.Equals(originDamage))
			mode = 0;
		else
			mode = -1;

		if (isPreview)
		{
			lobby.InstantiateDamageTMP(tr, damage.ToString(), mode, isMultiple);
		}
		else
		{
			if (isThornArmor)
				InGame.instance.playerInfo[enemy].TakeDamage(reducedDamage, reducedDamage);

			HP -= takenDamage;
			InGame.instance.buffSet[me].UpdateCount(CountType.takeDamage, -1);
			InGame.instance.InstantiateDamageTMP(tr, takenDamage.ToString(), mode, isMultiple);
			InGame.instance.inGameUI.UpdateHealth(me);

			Resource += resourceByTakeDamage;

			if (HP <= 0)
			{
				Debug.Log(string.Format("'{0}' 죽음", me.ToString()));
				isDead = true;
				InGame.instance.deadPlayerList.Add(me);
				InGame.instance.DelayDeath(me);
			}
		}

		return takenDamage;
	}

	public int DealDamage(CommandId id, int originDamage, bool isMultiple = false)
	{
		int realDamage;
		if (isPreview)
		{
			realDamage = lobby.pre_Enemy.TakeDamage(originDamage, originDamage, isMultiple);
		}
		else
		{
			int damage = Mathf.RoundToInt(originDamage * dealDamageMultiplier) + dealDamageBonus;
			if (canResourceToBonusDamage)
				damage += Resource;

			realDamage = InGame.instance.playerInfo[enemy].TakeDamage(damage, originDamage, isMultiple);
			InGame.instance.buffSet[me].UpdateCount(CountType.dealDamage, -1);

			Resource += resourceByDealDamage;

			if (InGame.instance.playingCommand.ContainsKey(me) && InGame.instance.playingCommand[me].id.Equals(id))
				InGame.instance.isCommandHit[me] = true;
		}

		return realDamage;
	}

	public int Restore(int amount)
	{
		if(isPreview)
		{
			lobby.InstantiateDamageTMP(tr, amount.ToString(), -2);
		}
		else
		{
			amount = Mathf.Clamp(amount, 0, maxHP - HP);
			HP += amount;
			InGame.instance.inGameUI.UpdateHealth(me);

			if (isVanish && !InGame.instance.me.Equals(me))
			{

			}
			else
			{
				if (amount != 0)
					InGame.instance.InstantiateDamageTMP(tr, amount.ToString(), -2);

			}
		}

		return amount;
	}

	public void LookEnemy()
	{
		if (isPreview)
		{
			tr.LookAt(lobby.pre_Enemy.tr);
		}
		else
		{
			tr.LookAt(InGame.instance.playerInfo[enemy].tr);
		}
	}

	public void SetAnimState(AnimState state)
	{
		if (!isDead)
		{
			animator.SetInteger("state", (int)state);
			lastAnimState = state;
		}
	}

}

public enum AnimState
{
	idle = 0, run = 1, earthStrike = 2, death = 3, whirlStrike = 4, stiff = 5, guard = 6,
	combatReady = 7, cutting = 8, scratch = 9, leapAttack = 10, innerWildness = 11,
	winner = 12, earthWave = 13, heartRip = 14, healPotion = 15, charge = 16,
	rapidShot = 17, flipShot = 18, startHunting = 19, hunterTrap = 20, paralyticArrow = 21,
	paralysis = 22, vanish = 23, curseStiff = 24, cursePoison = 25, spellFireExplosion = 26,
	spellLightning = 27, escapeSpell = 28, sniping = 29, sweep = 30, thornShield = 31,
	giantSwing = 32, bossRun = 33, incineration = 34, jumpAttack = 35, spinSwing = 36,
	harvest = 37, darkRedemption = 38,
}