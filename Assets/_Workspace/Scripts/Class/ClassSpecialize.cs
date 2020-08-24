using UnityEngine;

public class ClassSpecialize : MonoBehaviour
{
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
}
