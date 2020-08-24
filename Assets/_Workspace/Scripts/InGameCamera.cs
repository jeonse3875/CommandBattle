using DG.Tweening;
using UnityEngine;

public class InGameCamera : MonoBehaviour
{
    private Vector3 originPos = new Vector3(0, 41.55f, -44.43f);
    private Vector3 originRot = new Vector3(41.425f, 0, 0);
    private Vector3 zoomDistance = new Vector3(0, 24.4f, -25.2f);

    public float ZoomInTarget(Vector3 targetVec)
    {
        float zoomTime = 1f;
        transform.DOMove(targetVec + zoomDistance, zoomTime);

        return zoomTime;
    }

    public float ZoomOut()
    {
        float zoomTime = 1f;
        transform.DOMove(originPos, zoomTime);

        return zoomTime;
    }

    public void ResetCamera()
    {
        transform.position = originPos;
        transform.rotation = Quaternion.Euler(originRot);
    }
}
