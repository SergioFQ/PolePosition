using Mirror;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

/* PlayerController: Esta clase se encarga en todo el movimiento y físicas además de otras acciones como actualizar
 * por pantalla ciertos dato y mandar cierta información al server.
 */ 
public class PlayerController : NetworkBehaviour
{
    #region Variables

    [Header("Movement")] public List<AxleInfo> axleInfos;
    public float forwardMotorTorque = 100000;
    public float backwardMotorTorque = 50000;
    public float maxSteeringAngle = 15;
    public float engineBrake = 1e+12f;
    public float footBrake = 1e+24f;
    public float topSpeed = 200f;
    public float downForce = 100f;
    public float slipLimit = 0.2f; //coeficiente de rozamiento

    // Control vehiculo
    private float CurrentRotation { get; set; }
    private float InputAcceleration { get; set; }
    private float InputSteering { get; set; }
    private float InputBrake { get; set; }

    // SyncVar
    [SyncVar(hook = nameof(SetLap))] public int m_CurrentLap;
    [SyncVar(hook = nameof(FinalTotalTimeToString))] public string finalTotalTime = "";
    [SyncVar(hook = nameof(FinalBestLapTimeToString))] public string bestLapTime = "";

    // Public
    public UIManager m_UIManager;
    public bool isReady = false;
    public Vector3 posRanking;
    public int numVueltas;
    public Mutex mutexTimes = new Mutex();

    // Private
    private PlayerInfo m_PlayerInfo;
    private WheelFrictionCurve frictionCurve;
    private Rigidbody m_Rigidbody;
    private float m_SteerHelper = 0.8f;
    private float m_CurrentSpeed = 0;
    private PolePositionManager m_PolePositionManager;
    private CameraController m_cameraController;
    private bool debugUpsideDown = false;
    private Stopwatch[] LapTime;
    private Stopwatch totalTime = new Stopwatch();
    private TimeSpan timeSpan = new TimeSpan();

    


    #endregion Variables

    #region Unity Callbacks
    /* Awake: inicialización de variables y referencias a otras clases
     */ 
    public void Awake()
    {
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
        m_cameraController = FindObjectOfType<CameraController>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_CurrentLap = -1;
        frictionCurve = axleInfos[0].leftWheel.sidewaysFriction;
        frictionCurve.extremumSlip = 0.3f;
        LapTime = new Stopwatch[5];
        for (int i = 0; i < 5; i++)
            LapTime[i] = new Stopwatch();
        numVueltas = m_PolePositionManager.numVueltas;
    }

    /* Update: recoge los inputs del jugador, actualizamos el valor de la
     * velocidad y le pasamos el valor del tiempo de la vuelta actual al
     * delegado del tiempo por vuelta
     */ 
    public void Update()
    {
        
        InputAcceleration = Input.GetAxis("Vertical");
        InputSteering = Input.GetAxis(("Horizontal"));
        InputBrake = Input.GetAxis("Jump");
        Speed = m_Rigidbody.velocity.magnitude;
        TotalTimeDel = totalTime;
        if (m_CurrentLap < numVueltas && m_CurrentLap!=-1)
        {
            LapTimeDel = LapTime[m_CurrentLap];
        }
        if(m_CurrentLap == -1)
        {
            LapTimeDel = LapTime[0];
        }

        //Tecla respawn voluntaria
        if (Input.GetKeyUp(KeyCode.F) && isReady)
        {
            debugUpsideDown = true;
        }
    }

    /* OnCollisionEnter: detecta si el usuario ha chocado con partes de fuera del circuito y
     * procede a llamar al método CrashSpawn que se encargará de respawnear al player en en el 
     * circuito.
     */ 
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.transform.tag == "OutRace")
        {
            CrashSpawn();
        }
    }

    /* OnTriggerEnter: guarda cual es el último checkpoint por el que ha pasado el player. Con
     * esta información podemos hacer que el usuario aparezca en estas posiciones tras estrellarse
     * o volcar. Esta información también es usada para evitar que pueda hacer trampas asegurandonos
     * que ha recorrido todo el circuito.
     */ 
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Checkpoint")
        {
            int id = other.GetComponent<CheckpointsBehaviour>().ID;
            if ((id == m_PlayerInfo.LastPoint + 1) || (id == 0 && m_PlayerInfo.LastPoint == 12))
            {
                m_PlayerInfo.LastPoint = id;
            }
            if (id == 0 && m_PlayerInfo.CurrentLap == -1)
            {
                m_CurrentLap = 0;
            }
            if (id <= m_PlayerInfo.LastPoint - 1)
            {
                if (isLocalPlayer)
                    m_UIManager.SetWrongWay(true);
            }
            else
            {
                if (isLocalPlayer)
                    m_UIManager.SetWrongWay(false);
            }
            if (id <= m_PlayerInfo.LastPoint - 3 || (m_PlayerInfo.LastPoint <= 3 && id >= 8)) //mal recorrido
            {
                CrashSpawn();
            }
            
        }
    }

    /* FixedUpdate: realiza el cálculo de las físicas provocadas por los inputs
     * recogidos en el update
     */ 
    public void FixedUpdate()
    {
        InputSteering = Mathf.Clamp(InputSteering, -1, 1);
        InputAcceleration = Mathf.Clamp(InputAcceleration, -1, 1);
        InputBrake = Mathf.Clamp(InputBrake, 0, 1);
        float steering = maxSteeringAngle * InputSteering;
        foreach (AxleInfo axleInfo in axleInfos)
        {

            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }

            if (axleInfo.motor && isReady)
            {
                if (InputAcceleration > float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = forwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = forwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;

                }

                if (InputAcceleration < -float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (Math.Abs(InputAcceleration) < float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = 0;
                    axleInfo.leftWheel.brakeTorque = engineBrake;
                    axleInfo.rightWheel.motorTorque = 0;
                    axleInfo.rightWheel.brakeTorque = engineBrake;
                }

                if (InputBrake > 0)
                {
                    axleInfo.leftWheel.brakeTorque = footBrake;
                    axleInfo.rightWheel.brakeTorque = footBrake;
                }
                //si la velocidad es demasiado baja (estamos parados), subimos el rozamiento lateral para impedir la deriva del jugador. Una vez en moviemiento volverá a su valor inicial que es 0.2
                if (Math.Abs(axleInfo.leftWheel.attachedRigidbody.velocity.magnitude) < 0.25f)
                {
                    frictionCurve.extremumSlip = 0.3f;
                }
                else
                {
                    frictionCurve.extremumSlip = 0.2f;
                }



            }


            //asignamos el valor de la fricción lateral
            axleInfo.leftWheel.sidewaysFriction = frictionCurve;
            axleInfo.rightWheel.sidewaysFriction = frictionCurve;

            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

        SavingPosition();
        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        //TractionControl();
    }

    #endregion

    #region Methods

    /* SavingPosition: detecta si hemos pulsado la tecla F y de esta forma vuelca el coche. Además
     * si detecta que el coche está volcado hara que vuelva a la posición del último checkpoint
     */ 
    private void SavingPosition()
    {
        if (debugUpsideDown && isReady)
        {
            transform.Rotate(0, 0, 90);
            debugUpsideDown = false;
        }
        
        if (Vector3.Dot(transform.up, Vector3.down) > 0 && isReady)
        {
            CrashSpawn();
        }
    }

    /* CrashSpawn: Este método se encarga de hacer volver al jugador al último checkpoint por el que
     * haya pasado, haciendo también que el coche se oriente en la dirección del siguiente checkpoint.
     */ 
    private void CrashSpawn()
    {
        m_UIManager.SetWrongWay(false);
        Vector3 posAux;
        if (m_PlayerInfo.LastPoint == -1)
        {
            posAux = m_PolePositionManager.checkpoints[0].transform.position;
        }
        else
        {
            posAux = m_PolePositionManager.checkpoints[m_PlayerInfo.LastPoint].transform.position;
        }
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        transform.position = new Vector3(posAux.x, posAux.y + 3, posAux.z);
        transform.LookAt(m_PolePositionManager.checkpoints[(m_PlayerInfo.LastPoint + 1) % m_PolePositionManager.checkpoints.Length].transform.position);
    }


    /* SetInactiveByAbandonmet: en caso de que el resto de jugadores se vayan de la partida, pausaremos todos los tiempos del 
     * jugador restante y lo frenaremos.
     */ 
    public void setInactiveByAbandonmet()
    {
        totalTime.Stop();
        if(m_CurrentLap != -1 && m_CurrentLap < numVueltas)
        {
            LapTime[m_CurrentLap].Stop();
        }
        else
        {
            LapTime[0].Stop();
        }
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        isReady = false;
    }

    /* SetInactive: este método frenará al jugador una vez haya completado todas las vueltas, 
     * paramos sus tiempos totales y por vuelta y lo transportamos a la zona de podio
     */ 
    public void SetInactive()
    {
        totalTime.Stop();
        if (isLocalPlayer)
        {
            Stopwatch aux = LapTime[0];
            for(int i = 0; i < numVueltas; i ++)
            {
                if (LapTime[i].Elapsed.TotalMilliseconds < aux.Elapsed.TotalMilliseconds)
                {
                    aux = LapTime[i];
                }
            }
            m_PlayerInfo.FinalTime = (TimeToString(totalTime));
            m_PlayerInfo.BestLapTime = (TimeToString(aux));
            mutexTimes.WaitOne();
            CmdUpdateTime(m_PlayerInfo.FinalTime, m_PlayerInfo.BestLapTime);
            mutexTimes.ReleaseMutex();
        }

        WheelFrictionCurve friction = axleInfos[0].leftWheel.forwardFriction;
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_PolePositionManager.SetPosInRanking();
        transform.LookAt(m_PolePositionManager.target.transform.position);
        transform.position = posRanking;
        foreach (var axleInfo in axleInfos)
        {
            friction.extremumSlip = 100;
            axleInfo.leftWheel.motorTorque = 0;
            axleInfo.rightWheel.motorTorque = 0;
            axleInfo.leftWheel.brakeTorque = 1e+30f;
            axleInfo.rightWheel.brakeTorque = 1e+30f;
            axleInfo.leftWheel.forwardFriction = friction;
            axleInfo.rightWheel.forwardFriction = friction;
            friction.extremumSlip = 0.3f;
            axleInfo.rightWheel.sidewaysFriction = friction;
            axleInfo.leftWheel.sidewaysFriction = friction;
        }
        isReady = false;
    }

    /* StartTime: crea los tiempos totales y por vuelta y los activo.
     */ 
    public void StartTime()
    {
        totalTime = new Stopwatch();
        LapTime[0] = new Stopwatch();
        totalTime.Start();
        LapTime[0].Start();
    }
    /* TimeToString: pasamos a string los tiempos totales y por vuelta de los jugadores.
     */ 
    public string TimeToString(Stopwatch s)
    {
        timeSpan = s.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
             timeSpan.Minutes, timeSpan.Seconds,
             timeSpan.Milliseconds / 10);
        return elapsedTime;

    }
    /* IncreaseLap: llamamos a un Command que aumenta en 1 el número de vueltas del jugador con el ID que
     * es pasado por parámetro.
     */ 
    public void IncreaseLap(int id)
    {
        if (isLocalPlayer)
        {
            CmdIncreaseLap(id);
        }
    }

    // crude traction control that reduces the power to wheel if the car is wheel spinning too much
    private void TractionControl()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit wheelHitLeft;
            WheelHit wheelHitRight;
            axleInfo.leftWheel.GetGroundHit(out wheelHitLeft);
            axleInfo.rightWheel.GetGroundHit(out wheelHitRight);

            if (wheelHitLeft.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitLeft.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.leftWheel.motorTorque -= axleInfo.leftWheel.motorTorque * howMuchSlip * slipLimit;
            }

            if (wheelHitRight.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitRight.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.rightWheel.motorTorque -= axleInfo.rightWheel.motorTorque * howMuchSlip * slipLimit;
            }

        }
    }

    // this is used to add more grip in relation to speed
    private void AddDownForce()
    {
        foreach (var axleInfo in axleInfos)
        {
            axleInfo.leftWheel.attachedRigidbody.AddForce(
                -transform.up * (downForce * axleInfo.leftWheel.attachedRigidbody.velocity.magnitude));
        }
    }

    private void SpeedLimiter()
    {
        float speed = m_Rigidbody.velocity.magnitude;
        if (speed > topSpeed)
            m_Rigidbody.velocity = topSpeed * m_Rigidbody.velocity.normalized;
    }

    // Finds the corresponding visual wheel
    // Correctly applies the transform
    public void ApplyLocalPositionToVisuals(WheelCollider col)
    {
        if (col.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = col.transform.GetChild(0);
        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);
        var myTransform = visualWheel.transform;
        myTransform.position = position;
        myTransform.rotation = rotation;
    }

    private void SteerHelper()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit[] wheelHit = new WheelHit[2];
            axleInfo.leftWheel.GetGroundHit(out wheelHit[0]);
            axleInfo.rightWheel.GetGroundHit(out wheelHit[1]);
            foreach (var wh in wheelHit)
            {
                if (wh.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }
        }

        // this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
        if (Mathf.Abs(CurrentRotation - transform.eulerAngles.y) < 10f)
        {
            var turnAdjust = (transform.eulerAngles.y - CurrentRotation) * m_SteerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
        }

        CurrentRotation = transform.eulerAngles.y;
    }
    #endregion

    #region Delegates

    /* Delegado que se encargará de actualizar el valor de la velocidad por pantalla cuando note un cambio
     * de esta velocidad superior a 1. Haciendo que solo actualice cuando aumenta en 1, quitamos 
     * actualizaciones innecesarias en el HUD del usuario. Por pantalla el cambio por pantalla será siempre este 
     * cambio multiplicado por 5.
     */ 
    private float Speed
    {
        get { return m_CurrentSpeed; }
        set
        {
            if (Math.Abs(m_CurrentSpeed - value) < 1) return;
            m_CurrentSpeed = value;
            if (OnSpeedChangeEvent != null)
                OnSpeedChangeEvent(m_CurrentSpeed);
        }
    }

    public delegate void OnSpeedChangeDelegate(float newVal);

    public event OnSpeedChangeDelegate OnSpeedChangeEvent;

    /* Delegado que se encargará de actualizar por pantalla el tiempo total del jugador. Esta actualización 
     * se realizará siempre que el tiempo aumente en 1 milisegundo. 
     */ 
    private Stopwatch TotalTimeDel
    {
        get { return totalTime; }
        set
        {
            if (Math.Abs(totalTime.Elapsed.TotalMilliseconds - value.Elapsed.TotalMilliseconds) < 0.0001f) return;
            totalTime = value;
            if (OnTotalTimeChangeEvent != null)
                OnTotalTimeChangeEvent(totalTime);
        }
    }

    public delegate void OnTotalTimeChangeDelegate(Stopwatch newVal);

    public event OnTotalTimeChangeDelegate OnTotalTimeChangeEvent;

    /* Delegado que se encargará de actualizar por pantalla el tiempo por vuelta del jugador. Esta actualización 
     * se realizará siempre que el tiempo aumente en 1 milisegundo. 
     */
    private Stopwatch LapTimeDel
    {
        get { return LapTime[m_CurrentLap]; }
        set
        {
            if (Math.Abs(totalTime.Elapsed.TotalMilliseconds - value.Elapsed.TotalMilliseconds) < 0.0001f) return;
            if (m_CurrentLap == -1) LapTime[0] = value;
            else { LapTime[m_CurrentLap] = value; }
            if (OnLapTimeChangeEvent != null)
            {
                if (m_CurrentLap == -1)
                    OnLapTimeChangeEvent(LapTime[0]);
                else { OnLapTimeChangeEvent(LapTime[m_CurrentLap]); }
            }
        }
    }

    public delegate void OnLapTimeChangeDelegate(Stopwatch newVal);

    public event OnLapTimeChangeDelegate OnLapTimeChangeEvent;

    #endregion Delegates

    #region Commands & ClientRPCs

    /* CmdUpdateTime: Guardamos el tiempo total y la mejor vuelta del jugador.
     * Esto activará un Hook en el que se tramitará los tiempos de todos los
     * jugadores.
     */ 
    [Command]
    private void CmdUpdateTime(string time, string bestlapTime)
    {

        finalTotalTime = time;
        bestLapTime = bestlapTime;
    }
    /* CmdIncreaseLap: El server aumenta en 1 la vuelta actual del jugador con el 
     * id pasado por parámetro.
     */ 
    [Command]
    private void CmdIncreaseLap(int id)
    {
        m_CurrentLap++;
        m_PolePositionManager.m_Players[id].CurrentLap = m_CurrentLap;
    }

    /* CmdSetNumLaps: el cliente llama a este Command para que el servidor
     * le mande las vueltas elegidas por el servidor o host antes del inicio
     * de la carrera.
     */ 
    [Command]
    public void CmdSetNumLaps()
    {
        RpcSetNumLaps(numVueltas);        
    }
    /* RpcSetNumLaps: envía a todos los clientes el número de vueltas elegidas
     * previamente por el servidor o host.
     */ 
    [ClientRpc]
    private void RpcSetNumLaps(int laps)
    {
        numVueltas = laps;
        m_UIManager.textLaps.text = "Lap 0/" + laps;
    }

    #endregion Commands & ClientRPCs

    #region Hooks
    /* SetLap: Cada vez que un jugador aumente una vuelta, para
     * el tiempo de su vuelta actual, activa el tiempo de su nueva vuelta y 
     * aumenta en 1 su CurrentLap.
     */ 
    private void SetLap(int old, int newLap)
    {
        if (newLap > 0)
        {
            LapTime[newLap - 1].Stop();
            if (!(newLap >= numVueltas))
            {
                LapTime[newLap] = new Stopwatch();
                LapTime[newLap].Start();
            }
        }

        if (isLocalPlayer)
        {
            m_PlayerInfo.CurrentLap = newLap;
            m_UIManager.UpdateLap(m_PlayerInfo.CurrentLap);
        }
    }

    /* FinalTotalTimeToString: guarda el tiempo total del último 
     * jugador que haya llegado a meta y actualiza el HUD de todos 
     * los jugadores para ver el tiempo total de este.
     */ 
    private void FinalTotalTimeToString(string old, string newTime)
    {
        m_UIManager.UpdateTotalTimeRanking(newTime);
    }

    /* FinalBestLapTimeToString: guarda el mejor tiempo por vuelta del
     * jugador que acabe de entrar en meta y lo muestra en el HUD de todos 
     * los demás jugadores que hayan también terminado.
     */ 
    private void FinalBestLapTimeToString(string old, string newTime)
    {
        m_PlayerInfo.BestLapTime = newTime;
        m_UIManager.UpdateLapTimeRanking(m_PlayerInfo.BestLapTime);
    }
    #endregion Hooks
}