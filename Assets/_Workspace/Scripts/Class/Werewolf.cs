using UnityEngine;

public class Werewolf : ClassSpecialize
{
    public GameObject model_Human;
    public GameObject model_Wolf;
    public GameObject weapon_Human;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override Buff[] GetPassive()
    {
        Buff gainResource1 = new Buff(BuffCategory.gainResourceByDealDamage, true, 1);
        Buff gainResource2 = new Buff(BuffCategory.gainResourceByTakeDamage, true, 1);

        return new Buff[] { gainResource1, gainResource2 };
    }

    public override void DeTransform()
    {
        model_Wolf.SetActive(false);
        model_Human.SetActive(true);
        weapon_Human.SetActive(true);
    }

    public override void Transform()
    {
        model_Wolf.SetActive(true);
        model_Human.SetActive(false);
        weapon_Human.SetActive(false);
    }
}
