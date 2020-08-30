using UnityEngine;

public class Werewolf : ClassSpecialize
{
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

    public override void SetBaseStatus(PlayerInfo player)
    {
        player.maxHP = 200;
        player.HP = 200;
        player.resourceClamp = (0, 3);
        player.Resource = 0;
    }
}
