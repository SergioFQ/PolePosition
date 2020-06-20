using System;
using System.Threading;
using Mirror;
using UnityEngine;
using Random = System.Random;
using System.Diagnostics;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

/** SETUPLAYER CLASS
 * Esta clase se encarga de añadir cada nuevo jugador a la partida, controlando cada nueva conexión.
 * Desde el lado del cliente (host incluído) se genera un jugador, para ello se introduce la información adecuada a su playerInfo.
 * Desde el lado del servidor se añade también a su propia lista de jugadores.
 * Esta clase contiene también los commands encargados de pasar la información de playerInfo al servidor desde el cliente.
 * */
public class SetupPlayer : NetworkBehaviour
{
    #region Variables
    // Variables SyncVar
    [SyncVar (hook = nameof(SetName))] private string m_Name;
    [SyncVar (hook = nameof(SetColour))] private int m_Colour;
    [SyncVar] private int m_ID;

    // Referencias a scripts
    // Variable públicas
    public PlayerController m_PlayerController;
    public PlayerInfo m_PlayerInfo;
    public PolePositionManager m_PolePositionManager;
    public MyNetworkManager m_MyNetworkManager;

    // Variables privadas
    private UIManager m_UIManager;
    private NetworkManager m_NetworkManager;

    #endregion

    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked for NetworkBehaviour objects when they become active on the server.
    /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    /// </summary>
    /// 
    /* OnStartServer(): Este método es un override del OnStartServer() de NetworkBehaviour.
     * Si es serverOnly añade el player a la lista de m_players de PolePositionManager. También se actualiza el valor de m_ID a connectionToClient.connectionId - 1 pues el serverOnly se cuenta a sí mismo como connectionToClient.connectionId
     * Si no es serverOnly se activa el botón para que el host pueda modificar el número de vueltas
     * */
    public override void OnStartServer()
    {
        base.OnStartServer();
        if (isServerOnly)
        {
            if (!m_PolePositionManager.started && !m_PolePositionManager.full)
            {
                m_PolePositionManager.AddPlayer(m_PlayerInfo);
            }
            m_ID = connectionToClient.connectionId - 1;
        }
        else
        {
            m_ID = connectionToClient.connectionId;
            m_UIManager.buttonLaps.gameObject.SetActive(true);
        }
    }



    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    /// 
    /* OnStartClient(): Este método es un override del OnStartClient() de NetworkBehaviour.
     * Inicializa los valores comunes de de playerInfo, si la carrera no está empezada y no está llena añade el player a la lista de m_players de m_PolePositionManager
     * Si no se cumple esa condición y es localPlayer activa el HUD correspondiente para que el jugador sepa de la situación
     * */
    public override void OnStartClient()
    {
        base.OnStartClient();
        m_PlayerInfo.ID = m_PolePositionManager.m_Players.Count;
        m_PlayerInfo.CurrentLap = -1;
        m_PlayerInfo.LastPoint = -1;
        if (!m_PolePositionManager.started && !m_PolePositionManager.full)
        {
            m_PolePositionManager.AddPlayer(m_PlayerInfo);
        }
        else
        {
            if (isLocalPlayer)
            {
                m_UIManager.ActivateFullGameHUD();
                m_NetworkManager.StopClient();
                if (!isServer)
                {
                    m_UIManager.buttonLaps.gameObject.SetActive(false);
                    
                }
            }
        }
    }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    /// 
    /* OnStartLocalPlayer(): Este método es un override de OnStartLocalPlayer() de NetworkBehaviour.
     * Si la carrera no está empezada se llama a los commands encargados de establecer el nombre, color, id y vueltas del jugador en el servidor.
     * También se inicializa el valor de playerController.isReady a false para que no pueda moverse hasta que no comience la carrera.
     * */
    public override void OnStartLocalPlayer()
    {
        
        m_PolePositionManager.m_SetUpPlayer = this;
        if (!m_PolePositionManager.started)
        {
            CmdSelectName((m_UIManager.playerName == "") ? ("Player" + (m_ID )) : (m_UIManager.playerName));
            CmdSelectColor(m_UIManager.colorNumber);
            CmdSelectIdLap(m_PlayerInfo.ID);
            m_PlayerController.CmdSetNumLaps();
            m_PlayerInfo.IsReady = false;

        }
    }

    #endregion

    #region Unity Callbacks

    /* Awake(): establece las relaciones de esta clase con el resto de las necesarias.
     * Para ello o bien las busca en el componente o bien las busca en el proyecto.
     * También inicializa el valor de la referencia de m_MyNetworkManager con esta clase.
     */
    private void Awake()
    {
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_PlayerController = GetComponent<PlayerController>();
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
        m_MyNetworkManager = FindObjectOfType<MyNetworkManager>();
        m_MyNetworkManager.m_SetUpPlayer = this;
    }

    /* Start(): si es localPlayer activa su playerController.
     * También establece el método que se ejecuta cuando se activa el evento del delegate
     * y configura la cámara del jugador.
     */
    void Start()
    {
        if (isLocalPlayer)
        {
            m_PlayerController.enabled = true;
            m_PlayerController.OnSpeedChangeEvent += OnSpeedChangeEventHandler;
            m_PlayerController.OnTotalTimeChangeEvent += OnTotalChangeEventHandler;
            m_PlayerController.OnLapTimeChangeEvent += OnLapChangeEventHandler;
            ConfigureCamera();
        }
    }

    /* OnDestroy(): En caso de que caiga el servidor si es un cliente y local player activa el HUD que muestra por pantalla lo que ha ocurrido al jugador
     * Este método siempre se ejecuta cuando cae el servidor pues al hacerlo se destruyen los clientes
     */
    private void OnDestroy()
    {
        if (isLocalPlayer && !isServer)
        {
            m_UIManager.ActivateServerOutHUD();

        }
    }

    #endregion

    #region Methods

    /* ConfigureCamera(): establece el punto hacia donde debe orientarse la cámara, es decir, el gameobject player.
     */
    void ConfigureCamera()
    {
        if (Camera.main != null) Camera.main.gameObject.GetComponent<CameraController>().m_Focus = this.gameObject;
    }

    /* UnfocusCamera(Vector3 cameraPos, Vector3 targetPos): Se ejecuta una vez el jugador termina la carrera. 
     * Lo que consigue es que la cámara pare de estar enfocada en el jugador y pase a colocarse en otro punto del escenario (camerapos) orientada hacia el podio (mirando hacia el punto targetPos)
     */
    public void UnfocusCamera(Vector3 cameraPos, Vector3 targetPos)
    {
        Camera.main.gameObject.GetComponent<CameraController>().m_Focus = null;
        Camera.main.gameObject.transform.position = cameraPos;
        Camera.main.gameObject.transform.LookAt(targetPos);
    }
    #endregion

    #region Events Handler

    /* OnSpeedChangeEventHandler(float speed): cuando sucede el evento OnSpeedChangeEvent recibe el nuevo valor de la velocidad y lo actualiza por pantalla llamando al método correspondiente de UIManager 
     */
    void OnSpeedChangeEventHandler(float speed)
    {
        m_UIManager.UpdateSpeed((int) speed * 5); // 5 for visualization purpose (km/h)
    }

    /* OnTotalChangeEventHandler(Stopwatch tt): cuando sucede el evento OnTotalChangeEvent recibe el nuevo valor del tiempo y lo actualiza por pantalla llamando al método correspondiente de UIManager 
     * Como el método de UIManager recibe strings previo a su envío se pasa al método m_PlayerController.TimeToString que lo formatea para convertirlo en string
     */
    void OnTotalChangeEventHandler(Stopwatch tt)
    {
        m_UIManager.UpdateTotalTime(m_PlayerController.TimeToString(tt));
    }

    /* OnLapChangeEventHandler(Stopwatch tt): cuando sucede el evento OnLapChangeEvent recibe el nuevo valor del tiempo y lo actualiza por pantalla llamando al método correspondiente de UIManager
     * Al igual que ocurre en el método anterior previamente pasa por el método m_PlayerController.TimeToString para pasarlo a string
     */
    void OnLapChangeEventHandler(Stopwatch lt)
    {
        m_UIManager.UpdateLapTime(m_PlayerController.TimeToString(lt));
    }

    #endregion

    #region Commands & ClientRPCs

    /* CmdSelectName(string name): recibe el nombre que se le va a poner al jugador en el cliente para actualizarlo también en el servidor, junto con su ID en caso de ser serverOnly
     * También se cambia el valor de la syncVar m_Name, para que se ejecute su hook.
     */
    [Command]
    void CmdSelectName(string name)
    {
        if (isServerOnly)
        {
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].Name = name;
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].ID = m_ID;
        }
        m_Name = name;
    }

    /* CmdSelectColor(int colour): recibe el color que se le va a poner al jugador para cambiar el valor de la syncVar m_Colour, para que se ejecute su hook. 
     * Si el servidor es serverOnly se llama directamente al hook pues este sólo se ejecuta en los clientes cuando se cambia el valor de la variable
     */
    [Command]
    public void CmdSelectColor(int colour)
    {
        if (isServerOnly)
        {
            SetColour(0, colour);
        }
        m_Colour = colour;
    }

    /* CmdSelectIdLap(int id): Command que recibe el valor del ID que se le va a poner al jugador para cambiar een el servidor en su lista de m_Players. 
     * También se establece que su currentLap inicial sea -1 al igual que el último chackpoint por el que ha pasado
     */
    [Command]
    void CmdSelectIdLap(int id)
    {
        if (isServerOnly)
        {
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].ID = id;
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].CurrentLap = -1;
            m_PolePositionManager.m_Players[m_PolePositionManager.m_Players.Count - 1].LastPoint = -1;
        }
    }

    /* CmdAddNumPlayer(): Command que avisa al servidor de que un cliente más ha dado al botón ready. La syncVar que se modifica pertenece a polePositionManager y se protege con un mutex
     * para eliminar posibles problemas de concurrencia
     */
    [Command]
    public void CmdAddNumPlayer()
    {
        m_PolePositionManager.readyPlayer.WaitOne();
        m_PolePositionManager.numPlayers += 1;
        m_PolePositionManager.readyPlayer.ReleaseMutex();
    }

    /* CmdUpdateOrdenRanking(): Command que avisa al servidor que un cliente más ha llegado a la meta. La syncVar que se modifica pertenece a polePositionManager, se incrementa en uno y se protege con un mutex
     * para eliminar posibles problemas de concurrencia
     */
    [Command]
    public void CmdUpdateOrdenRanking()
    {
        m_PolePositionManager.inRankingPlayer.WaitOne();
        m_PolePositionManager.ordenRanking++;
        m_PolePositionManager.inRankingPlayer.ReleaseMutex();
    }

    /* CmdUpdateOrdenRanking(): Command que avisa al servidor que un cliente más ha llegado a la meta. La syncVar que se modifica pertenece a polePositionManager, se actualiza con el nombre del jugador que ha llegado
     * y se protege con un mutex para eliminar posibles problemas de concurrencia
     */
    [Command]
    public void CmdUpdateNamesRanking()
    {
        m_PolePositionManager.mutexNamesRanking.WaitOne();
        m_PolePositionManager.namesRanking += m_PlayerInfo.Name + "\n";
        m_PolePositionManager.mutexNamesRanking.ReleaseMutex();
    }

    /* CmdStarted(): Command que se llama desde los clientes para avisar al servidor de que la carrera ha empezado
     */
    [Command]
    public void CmdStarted()
    {
        m_PolePositionManager.started = true;
    }

    public static event Action<SetupPlayer, string> OnMessage;

    /* CmdSend(string message): recibe el mensaje que envía un cliente y se lo manda al resto en caso de que no esté vacío
     */
    [Command]
    public void CmdSend(string message)
    {
        if (message.Trim() != "")
            RpcReceive(message.Trim());
    }

    /* RpcReceive(string message): recibe el string donde se almacena el mensaje y se llama en el command anterior.
     * En caso de que ocurra el evento OnMessage invocará el método que tiene asociado, implementado en la clase MyChat
     */
    [ClientRpc]
    public void RpcReceive(string message)
    {
        OnMessage?.Invoke(this, message);
    }
    #endregion

    #region Hooks

    /* SetName(string old, string newName): Hook que se ejecuta cuando cambia el valor de la syncVar m_Name,
     * establece el nombre de playerInfo con el nuevo valor
     */
    void SetName(string old, string newName)
    {
        m_PlayerInfo.Name = newName;
    }

    /* SetColour(int old, int newColour): Hook que se ejecuta cuando cambia el valor de la variable m_Colour,
     * establece el int del color de playerInfo con el nuevo valor.
     * Para que se muestre en el juego del color indicado se establece como active el body del raceCar acorde con el color
     */
    void SetColour(int old, int newColour)
    {
        m_PlayerInfo.ColourID = newColour;

        switch (newColour)
        {
            case 0:
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(true);
                transform.Find("raceCar").Find("body_white").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_orange").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_green").gameObject.SetActive(false);
                break;
            case 1:
                transform.Find("raceCar").Find("body_green").gameObject.SetActive(true);
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_orange").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_white").gameObject.SetActive(false);
                break;
            case 2:
                transform.Find("raceCar").Find("body_orange").gameObject.SetActive(true);
                transform.Find("raceCar").Find("body_green").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_white").gameObject.SetActive(false);
                break;
            case 3:
                transform.Find("raceCar").Find("body_white").gameObject.SetActive(true);
                transform.Find("raceCar").Find("body_orange").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_red").gameObject.SetActive(false);
                transform.Find("raceCar").Find("body_green").gameObject.SetActive(false);
                break;
        }
    }
    #endregion
}