using UnityEngine;

public class PlayerInfo
{
	public bool isPreview;
	public LobbyUI lobby;
	public Who me;
	public Who enemy;
	public int x;
	public int y;
	public int maxHP;
	public int hp;
	public ClassType classType;

	public float takeDamageMultiplier = 1f;
	public float dealDamageMultiplier = 1f;

	public Transform tr;
	public Animator animator;
	public AnimState lastAnimState;
	public bool canAct = true;
	public bool isDead = false;

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
		maxHP = 100;
		hp = 100;
		Grid grid = new Grid(5, 5, 5f, new Vector3(-10, 0, -10));

		tr.position = grid.PosToVec3(Pos());
		animator = tr.GetComponent<Animator>();
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

	public void TakeDamage(int damage, int mode)
	{
		if (isPreview)
		{
			lobby.InstantiateDamageTMP(tr, damage.ToString(), mode);
		}
		else
		{
			int takenDamage = Mathf.RoundToInt(damage * takeDamageMultiplier);
			hp = Mathf.Clamp(hp - takenDamage, 0, maxHP);
			InGame.instance.buffSet[me].UpdateCount(CountType.getHit, -1);
			Debug.Log(string.Format("'{0}'이 {1}의 피해를 입음", me.ToString(), takenDamage.ToString()));
			InGame.instance.InstantiateDamageTMP(tr, takenDamage.ToString(), mode);
			InGame.instance.inGameUI.UpdateHealth(me);

			if (hp <= 0)
			{
				Debug.Log(string.Format("'{0}' 죽음", me.ToString()));
				isDead = true;
				InGame.instance.deadPlayerList.Add(me);
				InGame.instance.StopCommand(me);
				animator.SetInteger("state", 3);
			}
		}
	}

	public void DealDamage(int damage)
	{
		if (isPreview)
		{
			lobby.pre_Enemy.TakeDamage(damage, 0);
		}
		else
		{
			int dealtDamage = Mathf.RoundToInt(damage * dealDamageMultiplier);
			int mode;
			if (Mathf.Approximately(dealDamageMultiplier, 1f))
				mode = 0;
			else
				mode = 1;
			InGame.instance.playerInfo[enemy].TakeDamage(dealtDamage, mode);
			InGame.instance.buffSet[me].UpdateCount(CountType.hit, -1);
		}
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
	combatReady = 7
}