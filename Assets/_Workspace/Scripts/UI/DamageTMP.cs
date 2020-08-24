using TMPro;
using DG.Tweening;
using UnityEngine;

public class DamageTMP : MonoBehaviour
{
	private float destroyTime = 5f;
	public float targetSize = 1f;
	public float popTime = 0.1f;
	private TextMeshPro tMP;
	public string message;
	public SpriteRenderer swordIcon;
	public SpriteRenderer shieldIcon;

	public Sequence seq;
	public Ease ease1;
	public Ease ease2;

	public void SetEffect(int mode, bool isMultiple = false)
	{
		tMP = GetComponent<TextMeshPro>();
		tMP.text = message;

		Destroy(gameObject, destroyTime);

		// length
		if (message.Length.Equals(3))
		{
			swordIcon.transform.localPosition = new Vector3(-1.71f, 0.01f, 0);
			shieldIcon.transform.localPosition = new Vector3(-1.69f, 0.007f, -0.006f);
		}
		else
		{
			swordIcon.transform.localPosition = new Vector3(-1.266f, 0.01f, 0);
			shieldIcon.transform.localPosition = new Vector3(-1.246f, 0.007f, -0.006f);
		}

		// mode
		if (mode.Equals(-1))
		{
			shieldIcon.gameObject.SetActive(true);
		}
		else if (mode.Equals(0))
		{
			targetSize = 1.3f;
			popTime = 0.1f;
		}
		else if (mode.Equals(1))
		{
			swordIcon.gameObject.SetActive(true);
			targetSize = 1.8f;
			popTime = 0.15f;
		}

		// position
		Ease ease = Ease.Unset;

		if (isMultiple)
			ease = Ease.OutCubic;

		// Execute
		seq = DOTween.Sequence()
			.Append(transform.DOScale(targetSize, popTime).SetEase(ease1))
			.Append(transform.DOScale(1f, popTime).SetEase(ease2))
			.Append(tMP.DOFade(0, 1.1f).SetEase(Ease.InCubic))
			.Join(swordIcon.DOFade(0, 1.1f).SetEase(Ease.InCubic))
			.Join(shieldIcon.DOFade(0, 1.1f).SetEase(Ease.InCubic))
			.Insert(0f, transform.DOMoveY(transform.position.y + 7f, 3.5f).SetEase(ease));
	}
}
