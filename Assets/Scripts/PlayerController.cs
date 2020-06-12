﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
    private UIManager m_UIManager;
    private WheelFrictionCurve frictionCurve;//creamos la curva de fricción para eliminar la deriva
    private Rigidbody m_Rigidbody;
    private float m_SteerHelper = 0.8f;
    private float m_CurrentSpeed = 0;
    [SyncVar(hook = nameof(RpcSetLap))] public int m_CurrentLap;
    private PolePositionManager m_PolePositionManager;//usado para controlar cuando el jugador vuelca
    private CameraController m_cameraController;//usado para controlar cuando el jugador vuelca
    private bool debugUpsideDown = false;
    private Vector3 force;

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

    /*
    private float Lap
    {
        get { return m_CurrentLap; }
        set
        {
            m_CurrentLap = (int)value;
            if (OnLapChangeEvent != null)
                OnLapChangeEvent(m_CurrentLap);
        }
    }
    
    public delegate void OnLapChangeDelegate(int newVal);

    public event OnLapChangeDelegate OnLapChangeEvent;
    */

    #endregion Variables

    #region Unity Callbacks

    public void Awake()
    {
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
        m_cameraController = FindObjectOfType<CameraController>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_CurrentLap = m_PlayerInfo.CurrentLap;
        frictionCurve = axleInfos[0].leftWheel.sidewaysFriction;
        frictionCurve.extremumSlip = 0.2f;
        //pos = transform.position;
    }

    public void Update()
    {
        InputAcceleration = Input.GetAxis("Vertical");
        InputSteering = Input.GetAxis(("Horizontal"));
        InputBrake = Input.GetAxis("Jump");
        Speed = m_Rigidbody.velocity.magnitude;
        

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

            if (axleInfo.motor)
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

        //Debug.Log("Vuelta " +m_PlayerInfo.CurrentLap);
        //Debug.Log(m_PlayerInfo.LastPoint);
        SavingPosition();
        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        TractionControl();
    }

    #endregion

    #region Methods

    private void SavingPosition()
    {
        if (debugUpsideDown)
        {
            transform.Rotate(0, 0, 90);
            debugUpsideDown = false;
        }
        
        //si he chocado

        //si esta volcado
        if (Vector3.Dot(transform.up, Vector3.down) > 0)
        {
            /*Debug.Log("Player antes del golpe: " + transform.position);
            Debug.Log("Player ha vuelto: " + pos);
            Debug.Log("x: " + m_cameraController.nextPoint.x + "y: " + m_cameraController.nextPoint.y + "z: " + m_cameraController.nextPoint.z);*/

            /*transform.position = pos;
            transform.LookAt(m_cameraController.nextPoint);
            m_Rigidbody.velocity = new Vector3(0f, 0f, 0f);*/
            CrashSpawn();
        }
        //else
        //{

            /*Esto guardaría el último punto en el que el coche estaba en el circuito decentemente. 
             * Bueno el 7 está puesto de random habría que cuadrar la distancia con el ancho de la carretera
             */
          /*  if ((m_PolePositionManager.posSphere[m_PlayerInfo.ID] - transform.position).magnitude < 7)
            {

                pos = m_PolePositionManager.posSphere[m_PlayerInfo.ID];
            }

        }*/
    }
     private void CrashSpawn()
    {
        Vector3 posAux = m_PolePositionManager.checkpoints[m_PlayerInfo.LastPoint].transform.position;
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        transform.position = new Vector3(posAux.x,posAux.y + 3, posAux.z);       
        transform.LookAt(m_PolePositionManager.checkpoints[(m_PlayerInfo.LastPoint + 1) % m_PolePositionManager.checkpoints.Length].transform.position);
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.transform.tag == "OutRace")
        {
            //contador para que si a los 2 segundos o asi sigues golpeando que llame a la funcion
            force = m_Rigidbody.velocity;
                CrashSpawn();
            
        }

        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "Checkpoint")
        {
            //Debug.Log("hola");
            int id = other.GetComponent<CheckpointsBehaviour>().ID;
            if ((id == m_PlayerInfo.LastPoint + 1) || (id == 0 && m_PlayerInfo.LastPoint==12))
            {
                m_PlayerInfo.LastPoint = id;
                //pos = other.transform.position;
            }

            if (id <= m_PlayerInfo.LastPoint - 2 || (m_PlayerInfo.LastPoint <= 3 && id >= 8)) //comprobamos el hacer mal el recorrido
            {
                //Debug.Log("Habria que hacer Spawn");
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

    [ClientRpc]
    private void RpcSetLap(int old, int newLap)
    {
        m_PlayerInfo.CurrentLap = newLap;
        if (isLocalPlayer)
        {
            m_UIManager.UpdateLap(newLap);
        }
    }

    #endregion
}