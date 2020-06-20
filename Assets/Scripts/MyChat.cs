using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/* MyChat: clase que maneja el paso de mensajes en el chat que se activa en el menú de paso entre que se inicia el host/cliente hasta el comienzo de la partida
 */
public class MyChat : MonoBehaviour
{
    #region Variables

    // Variables públicas
    public InputField chatMessage;
    public Text chatHistory;
    public Scrollbar scrollbar;

    #endregion

    #region Unity Callbacks

    /* Awake(): SetupPlayer.OnMessage es una accion a la que se le pasa el método OnPlayerMessage para que se ejecute cuando ocurra el evento
     * Por tanto en el awake se establece seta relación entre el evento Action On Message y el método que se debe ejecutar
     */
    public void Awake()
    {
        SetupPlayer.OnMessage += OnPlayerMessage;
    }

    /* OnDestroy(): Cuando termina la partida se debe desestablecer esta relación para que, si los jugadores reinician el server volviendo al menu y empiezan una nueva partida sin cerrar las builds,
     * como no se rompe la relación pero sí el objeto, no se intente acceder a lo que ya ha sido destruido
     */
    public void OnDestroy()
    {
        SetupPlayer.OnMessage -= OnPlayerMessage;
    }

    #endregion

    #region Methods

    /* OnPlayerMessage(SetupPlayer player, string message): recibe el player y el mensaje, lo muestra de colores distintos según sea el local o no. 
     */
    private void OnPlayerMessage(SetupPlayer player, string message)
    {
        string prettyMessage = player.isLocalPlayer ?
            $"<color=red>{player.m_PlayerInfo.Name}: </color> {message}" :
            $"<color=blue>{player.m_PlayerInfo.Name}: </color> {message}";
        AppendMessage(prettyMessage);

        //Debug.Log(message);
    }

    /* OnSend(): envía el mensaje si tiene contenido, pasándolo al servidor mediante un Command. Reinicia el valor de la variable chatMessage 
     */
    public void OnSend()
    {
        if (chatMessage.text.Trim() == "")
            return;

        SetupPlayer player = NetworkClient.connection.identity.GetComponent<SetupPlayer>();

        player.CmdSend(chatMessage.text.Trim());
        chatMessage.text = "";
    }

    /* AppendMessage(string message): llama a la coroutine AppendAndScroll(string message), enviando el mensaje por parámetro 
     */
    internal void AppendMessage(string message)
    {
        StartCoroutine(AppendAndScroll(message));
    }

    /* AppendAndScroll(string message): Coroutine que actualiza el valor de la variable chatHistory con el nuevo mensaje para que se muestre por pantalla en el chat
     * También resetea el valor de la scrollbar a 0
     * El yield return null marca el punto donde la función se interrumpe para continuar su ejecución en el siguiente frame
     */
    IEnumerator AppendAndScroll(string message)
    {
        chatHistory.text += message + "\n";

        yield return null;
        yield return null;

        scrollbar.value = 0;
    }

    #endregion

}

