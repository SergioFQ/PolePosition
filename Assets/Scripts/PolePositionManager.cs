using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Mirror;
using UnityEngine;

public class PolePositionManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(numPlayersHook))] public int numPlayers;
    public NetworkManager networkManager;
    public Vector3[] posSphere; //vector publico que guardaría la posición de las esferas
    public readonly List<PlayerInfo> m_Players = new List<PlayerInfo>(4);
    private List<PlayerInfo> ordenP = new List<PlayerInfo>(4);
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    [SyncVar(hook = nameof(SetRaceOrder))] private string myRaceOrder = "";
    private UIManager m_UIManager;
    private float[] arcLengths;
    public GameObject[] checkpoints;
    public GameObject[] posRanking;
    public GameObject target;
    [SerializeField] private GameObject cameraRankingPos;
    //private PlayerInfo m_PlayerInfo; 
    public SetupPlayer m_SetUpPlayer;
    [SyncVar(hook = nameof(UpdateNamesRanking))] public string namesRanking = "";
    [SyncVar(hook = nameof(SetOrderRanking))] public int ordenRanking = 0;
    public Mutex readyPlayer = new Mutex();
    public Mutex inRankingPlayer = new Mutex();
    public Mutex mutexNamesRanking = new Mutex();
    [SyncVar] public bool started = false;
    [SyncVar] public bool full = false;
    private MyNetworkManager m_MyNetworkManager;
    private MyChat m_chat;

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        if (m_chat == null) m_chat = FindObjectOfType<MyChat>();
        posSphere = new Vector3[4];
        m_DebuggingSpheres = new GameObject[networkManager.maxConnections];
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            posSphere[i] = this.m_DebuggingSpheres[i].transform.position; // Se inicializa a la primera posición de la esfera en el circuito y se actualiza más abajo
        }
        m_UIManager = FindObjectOfType<UIManager>();
        m_UIManager.m_polePositionManager = this; //de esta forma sabemos la relacion de cada poleposition con ui manager de cada player
        if (isServer)
        {
            started = false;
            full = false;
        }
        m_MyNetworkManager = FindObjectOfType<MyNetworkManager>();
        //m_SetUpPlayer = FindObjectOfType<SetupPlayer>();
        //m_PlayerInfo = GetComponent<PlayerInfo>();
    }

    private void Update()
    {

        if (m_Players.Count == 0) { return; }
        UpdateRaceProgress();
    }

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        arcLengths = new float[m_Players.Count];
        if (m_Players.Count == 4)
        {
            full = true;
        }
    }
    /*public void activateChat()
    {

        if (m_chat == null) m_chat = FindObjectOfType<MyChat>();
    }*/
    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        float[] m_ArcLengths;

        public PlayerInfoComparer(float[] orden)
        {
            m_ArcLengths = orden;
        }

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

    [ClientRpc]
    private void RpcVictoryByAbandonment()
    {
        m_Players[0].GetComponent<PlayerController>().m_UIManager.ActivateEndingByAbandonment();
        m_Players[0].GetComponent<PlayerController>().setInactiveByAbandonmet();
    }

    [ClientRpc]
    public void RpcStoppedServer()
    {
        m_UIManager.ActivateServerOutHUD();
    }

    private void CheckEnoughPlayers()
    {
        if (m_Players.Count == 1 && started)
        {
            m_Players[0].GetComponent<PlayerController>().setInactiveByAbandonmet();
            RpcVictoryByAbandonment();
            //started = false;
            // m_Players[0].GetComponent<SetupPlayer>().CmdEndServerGame();
        }
    }


    public void UpdateRaceProgress()
    {
        ordenP = new List<PlayerInfo>();
        float[] orden = new float[4];
        if (isServer)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                orden[i] = arcLengths[i];
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
                    if (isServerOnly && (m_Players.Count>0))
                    {
                        RpcDeletePlayer(i);
                    }
                    full = false;
                    CheckEnoughPlayers();
                }
                else
                {
                    ComputeCarArcLength(m_Players[i].ID);
                }
            }



            for (int i = 0; i < arcLengths.Length; i++)
            {
                orden[i] = arcLengths[i];
            }

            ordenP.Sort(new PlayerInfoComparer(orden));

            myRaceOrder = " ";
            foreach (var _player in ordenP)
            {
                myRaceOrder += _player.Name + "\n";

            }
        }
    }

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

    public void startRace()
    {
        if (!started)
        {
            if (isServer)
            {
                //Interlocked.Increment(ref numPlayers);

                readyPlayer.WaitOne();
                numPlayers += 1;
                readyPlayer.ReleaseMutex();
                if (isServerOnly && numPlayers >= m_Players.Count)
                {
                    //started = true;
                    //readyPlayer.WaitOne();
                    //numPlayers = 0;
                    //readyPlayer.ReleaseMutex();
                }
            }
            else
            {
                m_SetUpPlayer.CmdAddNumPlayer();
            }
        }
    }

    public void EndServer()
    {
        //Debug.Log("DEBERIA ACTIVAR EL HUD DE FIN SERVER");
        m_UIManager.ActiveEndServer();
        networkManager.StopHost();
    }

    public bool CheckSpace()
    {
        return started;
    }

    [ClientRpc]
    private void RpcDeletePlayer(int id)
    {
        for (int i = id + 1; i < m_Players.Count; i++)
        {
            m_Players[i].ID--;
        }
        m_Players.RemoveAt(id);
    }

    public void RemovePlayer(int id)
    {

        for (int i = id + 1; i < m_Players.Count; i++)
        {
            m_Players[i].ID--;
        }
        m_Players.RemoveAt(id);
        takenPositions(id);

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

    public void takenPositions(int id)
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

    private void numPlayersHook(int old, int newValue)
    {
        if ((newValue >= m_Players.Count && m_Players.Count > 1) && !started)
        {
            started = true;
            if (m_SetUpPlayer != null)
            {
                m_SetUpPlayer.CmdStarted();
            }
            /*if (isServer)
            {
                started = true;
            }*/
            /*else
            {
                m_SetUpPlayer.CmdStarted();
            }*/
            //
            //m_SetUpPlayer.CmdStarted();
            //Debug.Log(m_Started + " " + started);
            if (m_SetUpPlayer != null && m_SetUpPlayer.m_PlayerController != null)
            {
                m_SetUpPlayer.m_PlayerController.isReady = true;
                m_SetUpPlayer.m_PlayerController.StartTime();
            }

            m_UIManager.deactivateReadyMenu();
            readyPlayer.WaitOne();
            //numPlayers = 0;
            readyPlayer.ReleaseMutex();
        }
    }


    //[ClientRpc]
    void ComputeCarArcLength(int ID)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this.m_Players[ID].transform.position;
        int segIdx;
        float carDist;
        Vector3 carProj;

        float minArcL =
            this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);

        this.m_DebuggingSpheres[ID].transform.position = carProj;
        posSphere[ID] = this.m_DebuggingSpheres[ID].transform.position;
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
    void SetRaceOrder(string old, string newOrder)
    {

        m_UIManager.UpdateNames(newOrder);
    }

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

    public void SetPosInRanking()
    {

        if (isServer)
        {
            //Interlocked.Increment(ref ordenRanking);
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

    private void SetOrderRanking(int old, int newOR)
    {
        m_SetUpPlayer.m_PlayerController.posRanking = posRanking[old].transform.position;
    }

    private void UpdateNamesRanking(string old, string newOR)
    {
        m_UIManager.UpdateRanking(newOR);
    }
    /*private void UpdateRaceStart( bool old, bool newStart)
    {
        m_Started = newStart;
    }*/
}