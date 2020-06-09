using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitController : MonoBehaviour
{
    private LineRenderer m_CircuitPath;
    private Vector3[] m_PathPos;
    private float[] m_CumArcLength;
    private float m_TotalLength;

    public float CircuitLength
    {
        get { return m_TotalLength; }
    }

    void Start()
    {
        m_CircuitPath = GetComponent<LineRenderer>();

        int numPoints = m_CircuitPath.positionCount;
        m_PathPos = new Vector3[numPoints];
        m_CumArcLength = new float[numPoints];
        m_CircuitPath.GetPositions(m_PathPos);

        // Compute circuit arc-length
        m_CumArcLength[0] = 0;

        for (int i = 1; i < m_PathPos.Length; ++i)
        {
            float length = (m_PathPos[i] - m_PathPos[i - 1]).magnitude;
            m_CumArcLength[i] = m_CumArcLength[i - 1] + length;
        }

        m_TotalLength = m_CumArcLength[m_CumArcLength.Length - 1];
    }

    public Vector3 GetSegment(int idx, out Vector3 nextPoint)//guardamos el siguiente punto para hacer que el jugador apunte alli al salirse de la pista o volcarse
    {
        nextPoint = m_PathPos[(idx + 1) % m_PathPos.Length];
        return m_PathPos[idx + 1] - m_PathPos[idx];
    }

    public float ComputeClosestPointArcLength(Vector3 posIn, out int segIdx, out Vector3 posProjOut, out float distOut)
    {
        int minSegIdx = 0;
        float minArcL = float.NegativeInfinity;
        float minDist = float.PositiveInfinity;
        Vector3 minProj = Vector3.zero;

        // Check segments for valid projections of the point
        for (int i = 0; i < m_PathPos.Length - 1; ++i)
        {
            Vector3 pathVec = (m_PathPos[i + 1] - m_PathPos[i]).normalized;
            float segLength = (m_PathPos[i + 1] - m_PathPos[i]).magnitude;


            Vector3 carVec = (posIn - m_PathPos[i]);
            float dotProd = Vector3.Dot(carVec, pathVec);

            if (dotProd < 0)
                continue;

            if (dotProd > segLength)
                continue; // Passed

            Vector3 proj = m_PathPos[i] + dotProd * pathVec;
            float dist = (posIn - proj).magnitude;
            if (dist < minDist)
            {
                minDist = dist;
                minProj = proj;
                minSegIdx = i;
                minArcL = m_CumArcLength[i] + dotProd;
            }
        }

        // If there was no valid projection check nodes
        if (float.IsPositiveInfinity(minDist)) //minDist == float.PositiveInfinity
        {
            for (int i = 0; i < m_PathPos.Length - 1; ++i)
            {
                float dist = (posIn - m_PathPos[i]).magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    minSegIdx = i;
                    minProj = m_PathPos[i];
                    minArcL = m_CumArcLength[i];
                }
            }
        }

        segIdx = minSegIdx;
        posProjOut = minProj;
        distOut = minDist;

        return minArcL;
    }
}