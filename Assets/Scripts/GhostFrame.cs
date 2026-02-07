using UnityEngine;

[System.Serializable]
public struct GhostFrame
{
    public float timestamp; // 기록 시점 (초 단위)
    public Vector3 position;
    public Quaternion rotation;

    public GhostFrame(float time, Vector3 pos, Quaternion rot)
    {
        this.timestamp = time;
        this.position = pos;
        this.rotation = rot;
    }
}