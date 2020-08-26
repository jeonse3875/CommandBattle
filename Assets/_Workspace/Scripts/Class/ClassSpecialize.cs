using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassSpecialize : MonoBehaviour
{
	public List<GameObject> weapon;

	public virtual void Initialize()
	{
		DeTransform();
	}

	public virtual void SetBaseStatus(PlayerInfo player)
	{

	}

	public virtual Buff[] GetPassive()
	{
		return null;
	}

	public virtual void Transform()
	{

	}

	public virtual void DeTransform()
	{

	}

	public virtual void HideWeapon(float hideTime)
	{
		StartCoroutine(HideWeaponRoutine(hideTime));

	}

	private IEnumerator HideWeaponRoutine(float hideTime)
	{
		if (weapon == null)
			yield break;


		foreach (var obj in weapon)
		{
			obj.SetActive(false);
		}

		yield return new WaitForSeconds(hideTime);

		foreach (var obj in weapon)
		{
			obj.SetActive(true);
		}
	}
}
