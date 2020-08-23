using UnityEngine;

public class ClassSpecialize : MonoBehaviour
{
    public virtual void Initialize()
    {
        DeTransform();
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
