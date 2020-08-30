using System.Collections;
using UnityEngine;

public class Trap : MonoBehaviour
{
	public CommandId id;

	private Who commander;
	private Who target;
	private (int x, int y) pos;
	private int damage;
	private Buff deBuff;
	private bool isPreview;

	private Animator animator;
	private float timer = 0f;
	private PlayerInfo targetInfo;
	private PlayerInfo commanderInfo;
	private bool canActive = true;

	private LobbyUI lobby;

	public void SetTrap((int x, int y) pos, Who target, int damage, Buff deBuff, bool isPreview = false)
	{
		this.target = target;
		this.pos = pos;
		this.damage = damage;
		this.deBuff = deBuff;
		this.isPreview = isPreview;
		this.animator = GetComponent<Animator>();

		if (target.Equals(Who.p1))
			commander = Who.p2;
		else
			commander = Who.p1;

		if (isPreview)
		{
			lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
			targetInfo = lobby.pre_Enemy;
			commanderInfo = lobby.pre_Player;
		}
		else
		{
			targetInfo = InGame.instance.playerInfo[target];
			commanderInfo = InGame.instance.playerInfo[commander];
		}
	}

	private void Update()
	{
		if (timer > 0.3f)
		{
			if(canActive && targetInfo.Pos().Equals(pos))
			{
				StartCoroutine(Active());
				canActive = false;
			}
		}
		else
		{
			timer += Time.deltaTime;
		}
	}

	private IEnumerator Active()
	{
		yield return new WaitForSeconds(0.1f);
		animator.SetBool("state", true);
		yield return new WaitForSeconds(0.1f);

		if (targetInfo.Pos().Equals(pos))
		{
			targetInfo.TakeDamage(damage, damage);

			if (deBuff != null)
			{
				if (isPreview)
				{
					deBuff.isPreview = true;

					if (target.Equals(Who.p1))
						lobby.pre_BuffSet1.Add(deBuff);
					else
						lobby.pre_BuffSet2.Add(deBuff);
				}
				else
				{
					InGame.instance.buffSet[target].Add(deBuff);
				}
			}
		}

		yield return new WaitForSeconds(0.3f);

		Destroy(this.gameObject);
	}
}
