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
	public float dealDamageMultiplier = 1f;
	public int resourceByDealDamage = 0;
	public int resourceByTakeDamage = 0;

	public Transform tr;
	public Animator animator;
	public AnimState lastAnimState;
	public bool isDead = false;
	public int transformCount = 0;
	public bool isUnstoppable = false;

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

		int takenDamage = Mathf.RoundToInt(damage * takeDamageMultiplier);
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
			HP -= takenDamage;
			InGame.instance.buffSet[me].UpdateCount(CountType.takeDamage, -1);
			Debug.Log(string.Format("'{0}'이 {1}의 피해를 입음", me.ToString(), takenDamage.ToString()));
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

	public int DealDamage(int originDamage, bool isMultiple = false)
	{
		int realDamage;
		if (isPreview)
		{
			realDamage = lobby.pre_Enemy.TakeDamage(originDamage, originDamage, isMultiple);
		}
		else
		{
			int damage = Mathf.RoundToInt(originDamage * dealDamageMultiplier);

			realDamage = InGame.instance.playerInfo[enemy].TakeDamage(damage, originDamage, isMultiple);
			InGame.instance.buffSet[me].UpdateCount(CountType.dealDamage, -1);

			Resource += resourceByDealDamage;
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
			InGame.instance.InstantiateDamageTMP(tr, amount.ToString(), -2);
			HP += amount;
			InGame.instance.inGameUI.UpdateHealth(me);
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
	rapidShot = 17, flipShot = 18, startHunting = 19, hunterTrap = 20,
}