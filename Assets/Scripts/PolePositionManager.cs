using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Mirror;
using UnityEngine;

/* PolePositionManager se activa en cuanto se crea el servidor
 * Se encarga de manejar las posiones de los jugadores respecto al circuito y el estado de la carrera
 * Es el script que controla el funcionamiento en sí del juego, es decir numero de vueltas, cuando empieza la carrera, etc
 * También maneja muchas de las comunicaciones servidor/cliente
 */

public class PolePositionManager : NetworkBehaviour
{
    #region Variables

    // SyncVars
    [SyncVar(hook = nameof(NumPlayersHook))] public int numPlayers;
    [SyncVar(hook = nameof(SetRaceOrder))] private string myRaceOrder = "";
    [SyncVar(hook = nameof(UpdateNamesRanking))] public string namesRanking = "";
    [SyncVar(hook = nameof(SetOrderRanking))] public int ordenRanking = 0;
    [SyncVar] public bool full = false;
    [SyncVar] public bool started = false;
    [SyncVar] public int numVueltas;

    // Public
    public NetworkManager networkManager;
    public readonly List<PlayerInfo> m_Players = new List<PlayerInfo>(4);
    public GameObject[] checkpoints;
    public GameObject[] posRanking;
    public GameObject target;
    public SetupPlayer m_SetUpPlayer;
    public Mutex readyPlayer = new Mutex();
    public Mutex inRankingPlayer = new Mutex();
    public Mutex mutexNamesRanking = new Mutex();

    // Private
    private List<PlayerInfo> ordenP = new List<PlayerInfo>(4);
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    private UIManager m_UIManager;
    private float[] arcLengths;
    private MyNetworkManager m_MyNetworkManager;

    #endregion Variables

    #region Unity Callbacks

    /* Awake(): de esta clase crea las esferas que sirven para determinar la posición del coche en relación al circuito
     * También se encarga de inicializar variables y establecer las referencias de los scripts necesarios
     */
    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        m_DebuggingSpheres = new GameObject[networkManager.maxConnections];
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            m_DebuggingSpheres[i].GetComponent<MeshRenderer>().enabled = false;
        }
        m_UIManager = FindObjectOfType<UIManager>();
        m_UIManager.m_polePositionManager = this; //de esta forma sabemos la relacion de cada poleposition con ui manager de cada player
        if (isServer)
        {
            started = false;
            full = false;
        }
        m_MyNetworkManager = FindObjectOfType<MyNetworkManager>();
        numVueltas = 3;
        m_UIManager.InitLaps();
    }

    // Update(): llama a UpdateRaceProgress() si hay algún jugador en la partida
    private void Update()
    {

        if (m_Players.Count == 0) { return; }
        UpdateRaceProgress();
    }

    #endregion Unity Callbacks

    #region Methods

    /* AddPlayer(PlayerInfo player): se encarga de añadir el nuevo jugador en la lista de jugadores e inicializar el array arclengths para que tenga el tamaño acorde
     * En caso de haber 4 jugadores establece full a true para que no puedan unirse más.
     * Esto ya se soluciona en networkManager que tiene un número máximo de conexiones, pero en caso de que se cambie también se limita de esta manera
     */
    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        arcLengths = new float[m_Players.Count];
        if (m_Players.Count == 4)
        {
            full = true;
        }
    }

    /* UpdateRaceProgress(): actualiza el estado de la carrera,  cambia el valor de la SyncVar myRaceOrder con el nuevo orden de los nombres de los jugadores según su posición.
     * Se crea una lista nueva en la que se copian los valores de m_players y esta la que va a realizar el sort.
     * También comprueba si algún jugador es null lo cual significaría que se ha ido un jugador de la partida y se llama a RemovePlayer(int id) enviando el id correspondiente
     * Es en este método también donde se comprueba si, al irse un jugador, sólo queda uno con lo cual terminaría la partida
     */
    public void UpdateRaceProgress()
    {
        ordenP = new List<PlayerInfo>();
        if (isServer)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                ordenP.Add(m_Players[i]);
            }

            for (int i = m_Players.Count - 1; i >= 0; i--)
            {
                if (m_Players[i] == null)
                {
                    if (isServer)
                    {

                        RemovePlayer(i);
                    }
                    full = false;
                    CheckEnoughPlayers();
                }
                else
                {
                    ComputeCarArcLength(m_Players[i].ID);
                }
            }

            ordenP.Sort(new PlayerInfoComparer(arcLengths));

            myRaceOrder = " ";
            foreach (var _player in ordenP)
            {
                myRaceOrder += _player.Name + "\n";
            }
        }
    }
    /* EndGame(): método que avisa al servidor y los jugadores que estén conectados de que ha acabado la partida, desactivando sus conexiones
     */
    public void EndGame()
    {
        if (isServer)
        {
            if (m_Players.Count > 0)
            {
                RpcStoppedServer();
                networkManager.StopHost();
            }
        }
        else
        {
            networkManager.StopClient();
        }
    }

    /* StartRace(): si la partida no está empezada aumenta en uno el valor de la SyncVar numPlayers (que almacena los jugadores que hayan pulsado a ready),
     * Este valor se protege con un semáforo para que no pueda ser sobreescrito. Esta variable es importante pues si falla podría darse el caso de que nunca pudiese comenzar la carrera
     * Si se ejecuta en un cliente también se avisa al servidor con un Command, que también protege su valor
     */
    public void StartRace()
    {
        if (!started)
        {
            if (isServer)
            {

                readyPlayer.WaitOne();
                numPlayers += 1;
                readyPlayer.ReleaseMutex();
            }
            else
            {
                m_SetUpPlayer.CmdAddNumPlayer();
            }
        }
    }

    /* EndServer(): Activa el HUD que muestra que se ha desconectado el server
     * Y lo desconecta llamando a la fuinción correspondiente de network manager.
     */
    public void EndServer()
    {
        m_UIManager.ActiveEndServer();
        networkManager.StopHost();
    }

    /* RemovePlayer(int id): recibe por parámetro el índice del jugador a eliminar de la lista
     * Decrementa en uno el valor de los ids de los jugadores que se encontraran en un puesto superior de la lista
     * y elimina al jugador
     * También llama a la funcion TakenPositions enviando el id.
     * Si aún hay jugadores en la partida envía a los clientes el id del jugador que tienen que eliminar de sus propias listas
     * Si ya no quedan jugadores llama a la función endServer para que desactive el servidor
     */
    public void RemovePlayer(int id)
    {

        for (int i = id + 1; i < m_Players.Count; i++)
        {
            m_Players[i].ID--;
        }
        m_Players.RemoveAt(id);
        TakenPositions(id);
        if (m_Players.Count > 0)
        {
            RpcDeletePlayer(id);
        }
        if (m_Players.Count == 0)
        {
            if (isServerOnly)
            {
                if (started == true)
                {
                    EndServer();
                }
            }
        }

    }

    /* TakenPositions(int id): el array positionsIDs de m_MyNetworkManager se plantea de forma que cada posición del array representa un startingPoint
     * y contiene el id del jugador que está posicionado en ella. Por tanto al eliminar un jugador de la lista también se debe avisar de que esa posición ha quedado vacía para que otro jugador que se una 
     * pueda posicionarse en ella.
     * Además, al haber modificado los ids de los jugadores de la lista tmbién se modifican los de este array
     */
    public void TakenPositions(int id)
    {
        for (int i = 0; i < 4; i++)
        {
            if (m_MyNetworkManager.positionsIDs[i] > id)
            {
                m_MyNetworkManager.positionsIDs[i]--;
            }
        }
        m_MyNetworkManager.positionsIDs[id] = -1;
    }

    /* SetNamesRanking(): Cambia el valor de la syncVar namesRanking cuando un player llega a la meta, para que se muestre en el HUD
     * Esta syncVar se protege mediante un mutex, pues es importante que no haya cabida a posibles problemas.
     * En caso de haberlos y que dos jugadores llegasen a la vez podría por ejemplo perderse el nombre de uno de ellos.
     */
    public void SetNamesRanking()
    {

        if (isServer)
        {
            mutexNamesRanking.WaitOne();
            namesRanking += m_SetUpPlayer.m_PlayerInfo.Name + "\n";
            mutexNamesRanking.ReleaseMutex();
        }
        else
        {

            m_SetUpPlayer.CmdUpdateNamesRanking();
        }
    }

    /*SetPosInRanking(): Cambia el valor de la syncVar ordenRanking cuando un player llega a la meta, 
     * esta variable contiene la última posción ocupada en el ranking para que dos jugadores no puedan situarse en la misma.
     * Sirve para posicionar sus coches en el lugar correspondiente del podio
     * La variable está protegida por un mutex pues pueden darse los mismos problemas que con la SyncVar namesRanking
     */
    public void SetPosInRanking()
    {

        if (isServer)
        {
            inRankingPlayer.WaitOne();
            ordenRanking++;
            inRankingPlayer.ReleaseMutex();
        }
        else
        {
            m_SetUpPlayer.m_PlayerController.posRanking = posRanking[ordenRanking].transform.position;
            m_SetUpPlayer.CmdUpdateOrdenRanking();
        }
    }

    /* SetNumLaps(int laps): Recibe el valor de la variable laps de UIManager. Suma 3 para que concuerde con las vueltas reales de la partida. 
     * En UIManager van de 0 a 2 según se pulse el botón que lo maneja y queremos que las opciones sean 3, 4 o 5 
     * Por tanto actualiza el valor de la syncVar numVueltas y avisa a los clientes de este cambio para que lo actualicen en su HUD y su playerController
     * En caso de ser serveronly no se actualiza el valor de numVueltas en PlayerController pues el servidor carece de este script
     */
    public void SetNumLaps(int laps)
    {
        laps += 3;
        numVueltas = laps;
        if (!isServerOnly)
        {
            m_SetUpPlayer.m_PlayerController.numVueltas = laps;
        }
        if (isServer)
        {
            RpcSetNumLaps(laps);
        }
    }
    // Compute the projection of the car position to the closest circuit 
    // path segment and accumulate the arc-length along of the car along
    // the circuit.
    void ComputeCarArcLength(int ID)
    {
        
        Vector3 carPos = this.m_Players[ID].transform.position;
        int segIdx;
        float carDist;
        Vector3 carProj;

        float minArcL =
            this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);

        this.m_DebuggingSpheres[ID].transform.position = carProj;
        if (this.m_Players[ID].CurrentLap == 0)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL += m_CircuitController.CircuitLength *
                       (m_Players[ID].CurrentLap - 1);
        }
        arcLengths[ID] = minArcL;
    }


    /* CheckEnoughPlayers(): Comprueba que solo queda un jugador en la partida para que llame a la función que deshabilita su movimiento
     * y muestre el HUD que avisa al jugador de la situación por pantalla
     */
    private void CheckEnoughPlayers()
    {
        if (m_Players.Count == 1 && started)
        {
            m_Players[0].GetComponent<PlayerController>().setInactiveByAbandonmet();
            RpcVictoryByAbandonment();
        }
    }

    #endregion Methods

    #region Comparer
    /* PlayerInfoComparer(): Clase que hereda de Comparer y sirve para determinar qué jugador se encuentra por delante en la carrera de otro y así poder ordenar la lista ordenP
     * */
    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        // Variable privada
        float[] m_ArcLengths;
        // Constructor
        public PlayerInfoComparer(float[] orden)
        {
            m_ArcLengths = orden;
        }

        /*Compare(PlayerInfo x, PlayerInfo y): Método override de Compare que recibe dos playersInfo y compara sus vueltas y posiciones en arcLengths
         * */
        public override int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (x.CurrentLap < y.CurrentLap)
            {
                return 1;
            }

            if (m_ArcLengths[x.ID] > m_ArcLengths[y.ID])
            {
                if (x.CurrentLap == y.CurrentLap && (x.LastPoint == -1 && y.LastPoint != -1))
                {
                    return 1;
                }
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
    #endregion

    #region ClientRPCs

    /* RpcVictoryByAbandonment: se llama cuando queda un único cliente
     * Se encarga de llamar a la función que activa el HUD que avisa al jugador de su victoria
     * También se llama a la función setInactiveByAbandonmet() que para tanto los tiempos como el coche del jugador, deshabilitando su movimiento
     */
    [ClientRpc]
    private void RpcVictoryByAbandonment()
    {
        m_Players[0].GetComponent<PlayerController>().m_UIManager.ActivateEndingByAbandonment();
        m_Players[0].GetComponent<PlayerController>().setInactiveByAbandonmet();
    }

    /* RpcStoppedServer: Se activa cuando el server se desconecta, sirve para avisar a los jugadores de la situación
     * LLama al método ActivateServerOutHUD que activa el HUD correspondiente
     */
    [ClientRpc]
    public void RpcStoppedServer()
    {
        m_UIManager.ActivateServerOutHUD();
    }

    /* RpcDeletePlayer(int id): Se ejecuta en los clientes cuando es llamada por el servidor, en el momento en que un jugador es nulo pues se ha desconectado
     * Sirve para bajar los IDs de los jugadores que se unieron posteriormente y eliminar al desconectado de la lista local
     * En caso de que se trate de un host borraría al jugador dos veces por tanto se comprueba que no sea server
     */
    [ClientRpc]
    private void RpcDeletePlayer(int id)
    {
        if (!isServer)
        {
            for (int i = id + 1; i < m_Players.Count; i++)
            {
                m_Players[i].ID--;
            }
            m_Players.RemoveAt(id);
        }
            
    }

    /* RpcSetNumLaps(int id): Actualiza en el cliente el numero de vueltas que va a tener la carrera, que puede cambiar según decida el server
     * Esta actualización se realiza tanto internamente en PlayerController como en el HUD para que el jugador sea consciente del cambio
     */
    [ClientRpc]
    private void RpcSetNumLaps(int laps)
    {
        m_SetUpPlayer.m_PlayerController.numVueltas = laps;
        numVueltas = laps;
        m_UIManager.textLaps.text = "Lap 0/" + laps;
    }

    #endregion ClientRPCs

    #region Hooks

    /* NumPlayersHook(int old, int newValue): Hook que comprueba si la carrera puede empezar,
     * Para ello se comprueba que el numero de jugadores que han pulsado el botón ready sea igual al número de jugadores conectados y que la partida no esté ya empezada
     * Si la condición se cumple se realiza un cmd que avise al servidor de que la carrera empieza y se habilita el movimiento de los coches para que puedan realizar el fixedUpdate de PlayerController
     * También se llama a la unción que empieza a contar el tiempo de la carrera de cada jugador y se desactiva el HUD de readyMenu
     */
    private void NumPlayersHook(int old, int newValue)
    {
        if ((newValue >= m_Players.Count && m_Players.Count > 1) && !started)
        {
            if (m_SetUpPlayer != null)
            {
                m_SetUpPlayer.CmdStarted();
            }

            if (m_SetUpPlayer != null && m_SetUpPlayer.m_PlayerController != null)
            {
                m_SetUpPlayer.m_PlayerController.isReady = true;
                m_SetUpPlayer.m_PlayerController.StartTime();
            }

            m_UIManager.deactivateReadyMenu();
        }
    }
    /* SetRaceOrder(string old, string newOrder): Hook que actualiza el orden de la carrera en el HUD,
     * se ejecuta cuando cambia la SyncVar myRaceOrder en el método UpdateRaceProgress()
     */
    private void SetRaceOrder(string old, string newOrder)
    {
        m_UIManager.UpdateNames(newOrder);
    }

    /* UpdateNamesRanking(string old, string newOR): Hook que se activa cuando cambia el valor de la SyncVar namesRanking,
     * Esta variable solo se actualiza cuando un jugador termina la carrera
     * Hace que todos los jugadores vean en HUD de ranking el orden en el que van llegando los jugadores
     */
    private void UpdateNamesRanking(string old, string newOR)
    {
        m_UIManager.UpdateRanking(newOR);
    }
    /* SetOrderRanking(int old, int newOR): Hook que se ejecuta cuando cambia el valor de la SyncVar orderRanking.
     * Esta variable contiene la última posición que ha sido ocupada del ranking. 
     * Sirve para colocar el coche del jugador que llega a la meta en su posición correspondiente
     */
    private void SetOrderRanking(int old, int newOR)
    {
        m_SetUpPlayer.m_PlayerController.posRanking = posRanking[old].transform.position;
    }

    #endregion Hooks

}