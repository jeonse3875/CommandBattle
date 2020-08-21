using UnityEngine;
using UnityEngine.UI;

public class LogBlock : MonoBehaviour
{
    public Text text_CommandName;
    public Text text_Description;
    public Image image_CommandIcon;

    public void SetBlock(Command command, string message)
    {
        text_CommandName.text = command.name;
        text_Description.text = message;
        image_CommandIcon.sprite = command.GetCommandIcon();
    }
}
