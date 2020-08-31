using UnityEngine;
using UnityEngine.UI;

public class PassiveBlock : MonoBehaviour
{
    public Text text_PassiveName;
    public Text text_Description;
    public Image image_PassiveIcon;
    public ClassType cType;

    private string passiveName = "";
    private string description = "";
    private string maxHP = "";

    public void SetBlock(ClassType cType)
    {
        this.cType = cType;

        switch (cType)
        {
            case ClassType.knight:
                maxHP = "250";
                passiveName = "철갑";
                description = "받는 피해가 10% 감소합니다.";
                break;
            case ClassType.werewolf:
                maxHP = "200";
                passiveName = "피의 갈증";
                description = "피해를 주거나 받을 때마다 피의 갈증이 해소됩니다. " +
                    "갈증이 총 3번 해소되면 턴이 끝날 때 늑대로 변신합니다. 늑대 상태에서는 강화된 커맨드를 사용합니다.";
                break;
            case ClassType.hunter:
                maxHP = "150";
                passiveName = "집중";
                description = "공격이 빗나갈 때마다 집중 스택을 1개 얻습니다.(최대 4개) " +
                    "공격이 적중할 경우 스택 1개당 5의 추가 피해를 입히고 스택이 초기화됩니다.";
                break;
            case ClassType.witch:
                maxHP = "150";
                passiveName = "마력 흡수";
                description = "'저주' 커맨드가 적중할 때마다 마력을 흡수합니다.(최대 5번) " +
                    "마력을 소모해 강력한 '마법' 커맨드를 사용할 수 있습니다.";
                break;
            default:
                break;
        }

        image_PassiveIcon.sprite = Resources.Load<Sprite>(string.Format("PassiveIcon/{0}", cType.ToString()));
        text_PassiveName.text = string.Format("패시브 : {0}", passiveName);
        text_Description.text = description;
    }

    public void Button_Detail()
    {
        LobbyUI lobby = GameObject.Find("LobbyUI").GetComponent<LobbyUI>();
        lobby.SetCharacterPreview(cType);
        lobby.image_PDetail_ClassIcon.sprite = Command.GetClassIcon(cType);
        lobby.text_PDetailClassName.text = Command.GetKoreanClassName(cType);
        lobby.text_PDetailHP.text = maxHP;
        lobby.text_PassiveDescription.text = text_Description.text;
        lobby.text_PassiveName.text = text_PassiveName.text;

        lobby.group_PassiveDetail.SetActive(true);
    }
}
