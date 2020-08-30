using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassSpecialize : MonoBehaviour
{
	public List<GameObject> weapon;
	public List<GameObject> weapon_Transform;

	public GameObject visiblePart;

	public GameObject body;
	public GameObject body_Transform;

	private bool isTransform = false;


	public virtual void Initialize()
	{
		SetDefault();
	}

	public virtual void SetBaseStatus(PlayerInfo player)
	{

	}

	public virtual Buff[] GetPassive()
	{
		return null;
	}

	public void SetDefault()
	{
		if (body_Transform != null)
			body_Transform.SetActive(false);

		if (body != null)
			body.SetActive(true);

		if (weapon_Transform != null)
		{
			foreach (var obj in weapon_Transform)
			{
				obj.SetActive(false);
			}
		}

		if (weapon != null)
		{
			foreach (var obj in weapon)
			{
				obj.SetActive(true);
			}
		}
	}

	public virtual void Transform()
	{
		BodySetActive(false);
		WeaponSetActive(false);
		isTransform = true;
		BodySetActive(true);
		WeaponSetActive(true);
	}

	public virtual void DeTransform()
	{
		BodySetActive(false);
		WeaponSetActive(false);
		isTransform = false;
		BodySetActive(true);
		WeaponSetActive(true);
	}

	public void WeaponSetActive(bool onOff)
	{
		if (!isTransform)
		{
			if (weapon == null)
				return;

			foreach (var obj in weapon)
			{
				obj.SetActive(onOff);
			}
		}
		else
		{
			if (weapon_Transform == null)
				return;

			foreach (var obj in weapon_Transform)
			{
				obj.SetActive(onOff);
			}
		}
	}

	public void BodySetActive(bool onOff)
	{
		if (!isTransform)
		{
			if (body == null)
				return;

			body.SetActive(onOff);
		}
		else
		{
			if (body_Transform == null)
				return;

			body_Transform.SetActive(onOff);
		}
	}

	public void HideWeapon(float hideTime)
	{
		StartCoroutine(HideWeaponRoutine(hideTime));
	}

	private IEnumerator HideWeaponRoutine(float hideTime)
	{
		WeaponSetActive(false);

		yield return new WaitForSeconds(hideTime);

		WeaponSetActive(true);
	}

	public virtual void Vanish()
	{
		WeaponSetActive(false);
		visiblePart.SetActive(false);
	}

	public virtual void UnVanish()
	{
		WeaponSetActive(true);
		visiblePart.SetActive(true);
	}
}
