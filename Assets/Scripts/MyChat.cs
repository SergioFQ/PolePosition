using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class MyChat : MonoBehaviour
{
    #region Variables

    // Variables públicas
    public InputField chatMessage;
    public Text chatHistory;
    public Scrollbar scrollbar;

    #endregion

    #region Unity Callbacks

    public void Awake()
    {
        SetupPlayer.OnMessage += OnPlayerMessage;
    }

    public void OnDestroy()
    {
        SetupPlayer.OnMessage -= OnPlayerMessage;
    }

    #endregion

    #region Methods

    private void OnPlayerMessage(SetupPlayer player, string message)
    {
        string prettyMessage = player.isLocalPlayer ?
            $"<color=red>{player.m_PlayerInfo.Name}: </color> {message}" :
            $"<color=blue>{player.m_PlayerInfo.Name}: </color> {message}";
        AppendMessage(prettyMessage);

        Debug.Log(message);
    }


    public void OnSend()
    {
        if (chatMessage.text.Trim() == "")
            return;

        SetupPlayer player = NetworkClient.connection.identity.GetComponent<SetupPlayer>();

        player.CmdSend(chatMessage.text.Trim());
        chatMessage.text = "";
    }

    internal void AppendMessage(string message)
    {

        StartCoroutine(AppendAndScroll(message));
    }

    IEnumerator AppendAndScroll(string message)
    {
        chatHistory.text += message + "\n";

        yield return null;
        yield return null;

        scrollbar.value = 0;
    }

    #endregion

}

