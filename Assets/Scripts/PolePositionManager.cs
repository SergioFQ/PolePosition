using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

public class PolePositionManager : NetworkBehaviour
{
    public int numPlayers;
    public NetworkManager networkManager;
    public Vector3[] posSphere; //vector publico que guardaría la posición de las esferas aunque en teoría solo se necesitaría la propia
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>(4);
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    [SyncVar(hook = nameof(setRaceOrder))] private string myRaceOrder = "";
    private UIManager m_UIManager;


    
    private string RaceOrder
    {
        get { return myRaceOrder; }
        set
        {
            if (OnPlayerCountChangeEvent != null)
                OnPlayerCountChangeEvent(myRaceOrder);
        }
    }

    public delegate void OnPlayerCountChangeDelegate(string newVal);

    public event OnPlayerCountChangeDelegate OnPlayerCountChangeEvent;

    void OnPlayerCountChangeEventHandler(string name)
    {
        m_UIManager.UpdateNames(name);
    }

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
        posSphere = new Vector3[4];
        m_DebuggingSpheres = new GameObject[networkManager.maxConnections]; 
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
            posSphere[i] = this.m_DebuggingSpheres[i].transform.position; // Se inicializa a la primera posición de la esfera en el circuito y se actualiza más abajo
        }
        m_UIManager = FindObjectOfType<UIManager>();
        this.OnPlayerCountChangeEvent += OnPlayerCountChangeEventHandler;
    }

    private void Update()
    {
        if (m_Players.Count == 0)
            return;

        UpdateRaceProgress();
    }

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);

    }

    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        float[] m_ArcLengths;

        public PlayerInfoComparer(float[] arcLengths)
        {
            m_ArcLengths = arcLengths;
        }

        public override int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (this.m_ArcLengths[x.ID] > m_ArcLengths[y.ID])
            {
                //Debug.Log("x" +(int)this.m_ArcLengths[x.ID] + " 2 " + this.m_ArcLengths[x.ID]);
                //Debug.Log("y" +(int)m_ArcLengths[y.ID] + " 2 " + m_ArcLengths[y.ID]);
                return 1;
            }
            else {
                return -1; 
            }
        }
    }

    public void UpdateRaceProgress()
    {

        // Update car arc-lengths
        float[] arcLengths = new float[m_Players.Count];

        for (int i = 0; i < m_Players.Count; ++i)
        {
            Debug.Log(m_Players[i].ID);
            arcLengths[i] = ComputeCarArcLength(i);
        }
        if (isServer)
        {
            m_Players.Sort(new PlayerInfoComparer(arcLengths));
        }

        myRaceOrder = " ";
        int aux = 0;
        foreach (var _player in m_Players)
        {
            myRaceOrder += _player.Name + " " + arcLengths[aux] + "\n";
            aux++;
        }
        setRaceOrder("", myRaceOrder);
        //RaceOrder = myRaceOrder;
        


        /*
        m_Players.Sort(new PlayerInfoComparer(arcLengths));
        myRaceOrder = " ";
        foreach (var _player in m_Players)
        {
            myRaceOrder += _player.Name + "\n";
        }
        setRaceOrder("", myRaceOrder);
         
         */
        //Debug.Log("El orden de carrera es: " + myRaceOrder);
    }

    float ComputeCarArcLength(int ID)
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
        posSphere[ID] = this.m_DebuggingSpheres[ID].transform.position; //actualización de la posición de la esfera por ID, esto sí funciona
        if (this.m_Players[ID].CurrentLap == 0 || this.m_Players[ID].CurrentLap == -1)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL += m_CircuitController.CircuitLength *
                       (m_Players[ID].CurrentLap - 1);
        }

        return minArcL;
    }

    void setRaceOrder(string old, string newOrder)
    {
        RaceOrder = newOrder;
    }
}