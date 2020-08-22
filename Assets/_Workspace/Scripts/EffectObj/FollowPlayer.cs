using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
	public Transform target;

	private void Update()
	{
		if (target != null)
		{
			transform.position = target.position;
		}
	}
}
