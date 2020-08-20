using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommandButton : MonoBehaviour
{
    public Command command;
    public Button button;
    public Image image_CommandIcon;
    public Text text_Time;
    public Text text_Left;

    public void InitializeButton(CommandId id)
    {
        command = Command.FromId(id);
        image_CommandIcon.sprite = command.GetCommandIcon();

        if (command.limit.Equals(10))
            text_Left.text = "제한없음";
        else
            text_Left.text = "X " + command.limit.ToString();

        text_Time.text = command.time.ToString();
    }

    public void SetUnuseButton()
    {
        gameObject.SetActive(false);
    }
}
