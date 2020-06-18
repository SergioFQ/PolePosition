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

    private float CurrentRotation { get; set; }
    private float InputAcceleration { get; set; }
    private float InputSteering { get; set; }
    private float InputBrake { get; set; }

    private PlayerInfo m_PlayerInfo;
    public UIManager m_UIManager;
    private WheelFrictionCurve frictionCurve;//creamos la curva de fricción para eliminar la deriva
    private Rigidbody m_Rigidbody;
    private float m_SteerHelper = 0.8f;
    private float m_CurrentSpeed = 0;
    [SyncVar(hook = nameof(SetLap))] public int m_CurrentLap;
    private PolePositionManager m_PolePositionManager;//usado para controlar cuando el jugador vuelca
    private CameraController m_cameraController;//usado para controlar cuando el jugador vuelca
    private bool debugUpsideDown = false;
    public bool isReady = false;//variable que se usará para activar todos los coches a la vez
    public Vector3 posRanking;
    private Stopwatch[] LapTime = new Stopwatch[2];
    private Stopwatch totalTime = new Stopwatch();
    private TimeSpan timeSpan = new TimeSpan();
    [SyncVar(hook = nameof(FinalTotalTimeToString))] public string finalTotalTime = "";
    [SyncVar(hook = nameof(FinalBestLapTimeToString))] public string bestLapTime = "";
    Mutex mutexTimes = new Mutex();

    private float Speed
    {
        get { return m_CurrentSpeed; }
        set
        {
            if (Math.Abs(m_CurrentSpeed - value) < float.Epsilon) return;
            m_CurrentSpeed = value;
            if (OnSpeedChangeEvent != null)
                OnSpeedChangeEvent(m_CurrentSpeed);
        }
    }

    public delegate void OnSpeedChangeDelegate(float newVal);

    public event OnSpeedChangeDelegate OnSpeedChangeEvent;

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

    private Stopwatch LapTimeDel
    {
        get { return LapTime[m_CurrentLap]; }
        set
        {
            if (Math.Abs(totalTime.Elapsed.TotalMilliseconds - value.Elapsed.TotalMilliseconds) < 0.0001f) return;
            if(m_CurrentLap == -1) LapTime[0] = value;
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


    #endregion Variables

    #region Unity Callbacks

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
        for (int i = 0; i < 2; i++)
            LapTime[i] = new Stopwatch();
        //pos = transform.position;
    }


    public void Update()
    {
        
        InputAcceleration = Input.GetAxis("Vertical");
        InputSteering = Input.GetAxis(("Horizontal"));
        InputBrake = Input.GetAxis("Jump");
        Speed = m_Rigidbody.velocity.magnitude;
        TotalTimeDel = totalTime;
        if (m_CurrentLap < LapTime.Length && m_CurrentLap!=-1)
        {
            LapTimeDel = LapTime[m_CurrentLap];
        }
        if(m_CurrentLap == -1)
        {
            LapTimeDel = LapTime[0];
        }

        //Debug volcar y spawnear cuando salimos de la pista
        if (Input.GetKeyUp(KeyCode.F))
        {
            debugUpsideDown = true;
        }
    }

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
                    frictionCurve.extremumSlip = 0.3f;//nuevo valor rozamiento
                }
                else
                {
                    frictionCurve.extremumSlip = 0.2f;
                }



            }


            //asignamos el valor de la fricción lateral
            axleInfo.leftWheel.sidewaysFriction = frictionCurve;
            axleInfo.rightWheel.sidewaysFriction = frictionCurve;

            ApplyLocalPositionToVisuals(axleInfo.leftWheel);//las ruedas estan al reves nombradas (es como si se viesen de frente y no de espaldas)
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }
        //Transform pos = m_Rigidbody.transform;
        //transform.position = pos.position;
        CmdUpdatePos(m_PlayerInfo.ID, frictionCurve.extremumSlip);

        SavingPosition();
        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        //TractionControl();
    }

    #endregion

    #region Methods


    private void SavingPosition()
    {
        if (debugUpsideDown && isReady)
        {
            transform.Rotate(0, 0, 90);
            debugUpsideDown = false;
        }

        //si esta volcado
        if (Vector3.Dot(transform.up, Vector3.down) > 0 && isReady)
        {
            CrashSpawn();
        }
    }
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

    private void OnCollisionEnter(Collision collision)
    {

        if (collision.transform.tag == "OutRace")
        {
            CrashSpawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Checkpoint")
        {
            int id = other.GetComponent<CheckpointsBehaviour>().ID;
            if ((id == m_PlayerInfo.LastPoint + 1) || (id == 0 && m_PlayerInfo.LastPoint == 12))
            {
                m_PlayerInfo.LastPoint = id;
                //pos = other.transform.position;
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
            if (id <= m_PlayerInfo.LastPoint - 3 || (m_PlayerInfo.LastPoint <= 3 && id >= 8)) //comprobamos el hacer mal el recorrido
            {
                CrashSpawn();
            }
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

    // finds the corresponding visual wheel
    // correctly applies the transform
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

    private void SetLap(int old, int newLap)
    {
        //if (newLap == 0) { return; }
        if (newLap > 0)
        {
            LapTime[newLap - 1].Stop();
            if (!(newLap >= LapTime.Length))
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

    public void setInactiveByAbandonmet()
    {
        totalTime.Stop();
        if(m_CurrentLap != -1)
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


    public void SetInactive()
    {
        totalTime.Stop();
        if (isLocalPlayer)
        {
            Stopwatch aux = LapTime[0];
            foreach (var lap in LapTime)
            {
                if (lap.Elapsed.TotalMilliseconds < aux.Elapsed.TotalMilliseconds)
                {
                    aux = lap;
                }
            }
            m_PlayerInfo.FinalTime = (TimeToString(totalTime));
            m_PlayerInfo.BestLapTime = (TimeToString(aux));
            mutexTimes.WaitOne();
            CmdUpdateTime(m_PlayerInfo.FinalTime, m_PlayerInfo.BestLapTime);
            mutexTimes.ReleaseMutex();
        }
        //print(finalTotalTime);
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

    [Command]
    private void CmdUpdateTime(string time, string bestlapTime)
    {

        finalTotalTime = time;
        bestLapTime = bestlapTime;
    }

    [Command]
    private void CmdIncreaseLap(int id)
    {
        m_CurrentLap++;
        m_PolePositionManager.m_Players[id].CurrentLap = m_CurrentLap;
    }

    [ClientRpc]
    private void RpcUpdatePos (int id, float frictionCurve)
    {
        WheelFrictionCurve aux = m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[0].leftWheel.sidewaysFriction;
        aux.extremumSlip = frictionCurve;
        m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[0].leftWheel.sidewaysFriction = aux;
        m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[0].rightWheel.sidewaysFriction = aux;
        m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[1].leftWheel.sidewaysFriction = aux;
        m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[1].rightWheel.sidewaysFriction = aux;
        //if (m_PlayerInfo.ID != id){}
    }

    [Command]
    private void CmdUpdatePos (int id, float frictionCurve)
    {
        if(m_PolePositionManager.m_Players[id] != null)
        {
            WheelFrictionCurve aux = m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[0].leftWheel.sidewaysFriction;
            aux.extremumSlip = frictionCurve;
            m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[0].leftWheel.sidewaysFriction = aux;
            m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[0].rightWheel.sidewaysFriction = aux;
            m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[1].leftWheel.sidewaysFriction = aux;
            m_PolePositionManager.m_Players[id].GetComponent<PlayerController>().axleInfos[1].rightWheel.sidewaysFriction = aux;

            RpcUpdatePos(id, frictionCurve);
        }
        
    }

    public void StartTime()
    {
        totalTime = new Stopwatch();
        LapTime[0] = new Stopwatch();
        totalTime.Start();
        LapTime[0].Start();
    }



    public string TimeToString(Stopwatch s)
    {
        timeSpan = s.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
             timeSpan.Minutes, timeSpan.Seconds,
             timeSpan.Milliseconds / 10);
        return elapsedTime;

    }

    private void FinalBestLapTimeToString(string old, string newTime)
    {
        m_PlayerInfo.BestLapTime = newTime;
        m_UIManager.UpdateLapTimeRanking(m_PlayerInfo.BestLapTime);
    }

    private void FinalTotalTimeToString(string old, string newTime)
    {
        m_UIManager.UpdateTotalTimeRanking(newTime);
    }

    public void IncreaseLap(int id)
    {
        if (isLocalPlayer)
        {
            CmdIncreaseLap(id);
        }
    }

    
    #endregion
}