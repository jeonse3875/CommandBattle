using TMPro;
using DG.Tweening;
using UnityEngine;

public class DamageTMP : MonoBehaviour
{
	private float moveSpeed = 2f;
	private float destroyTime = 5f;
	public float targetSize = 1f;
	public float popTime = 0.1f;
	private TextMeshPro tMP;
	public string message;
	public SpriteRenderer swordIcon;
	private float timer = 0f;

	public Sequence seq;
	public Ease ease1;
	public Ease ease2;

	void Start()
	{
		tMP = GetComponent<TextMeshPro>();
		tMP.text = message;

		Destroy(gameObject, destroyTime);

		seq = DOTween.Sequence()
			.Append(transform.DOScale(targetSize, popTime).SetEase(ease1))
			.Append(transform.DOScale(1f, popTime).SetEase(ease2))
			.Append(tMP.DOFade(0, 1.3f).SetEase(Ease.InCubic))
			.Join(swordIcon.DOFade(0, 1.3f).SetEase(Ease.InCubic));
	}

	void Update()
	{
		transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);

		timer += Time.deltaTime;
	}

	public void SetEffect(int mode)
	{
		if (mode.Equals(0))
		{
			targetSize = 1.3f;
			popTime = 0.1f;
		}
		else if (mode.Equals(1))
		{
			targetSize = 1.6f;
			popTime = 0.15f;
		}
	}
}
