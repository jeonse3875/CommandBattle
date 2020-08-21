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

	public float takenDamageMultiplier = 1f;
	public float damageDealingMultiplier = 1f;

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

	public void GetDamage(int damage)
	{
		if (isPreview)
		{
			lobby.InstantiateDamageTMP(tr.position + Vector3.up * 4f, damage.ToString());
		}
		else
		{
			int takenDamage = Mathf.RoundToInt(damage * takenDamageMultiplier);
			hp = Mathf.Clamp(hp - takenDamage, 0, maxHP);
			Debug.Log(string.Format("'{0}'이 {1}의 피해를 입음", me.ToString(), takenDamage.ToString()));
			InGame.instance.InstantiateDamageTMP(tr.position + Vector3.up * 4f, takenDamage.ToString());
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
}