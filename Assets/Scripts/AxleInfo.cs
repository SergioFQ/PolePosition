using UnityEngine;

/* AxleInfo: clase que guarda variables que serán usadas para el moviemiento de
 * las ruedas del vehiculo.
 */ 
[System.Serializable]
public class AxleInfo
{
    #region Variables

    // Variables públicas
    public WheelCollider leftWheel;
    public WheelCollider rightWheel;
    public bool motor;
    public bool steering;

    #endregion
}