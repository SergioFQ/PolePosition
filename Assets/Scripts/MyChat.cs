using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


    public class MyChat : MonoBehaviour
    {
        public InputField chatMessage;
        public Text chatHistory;
        public Scrollbar scrollbar;

        public void Awake()
        {
            SetupPlayer.OnMessage += OnPlayerMessage;
        }

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

            // get our player
            SetupPlayer player = NetworkClient.connection.identity.GetComponent<SetupPlayer>();

            // send a message
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

            // it takes 2 frames for the UI to update ?!?!
            yield return null;
            yield return null;

            // slam the scrollbar down
            scrollbar.value = 0;
        }
    }

