using UnityEngine;

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