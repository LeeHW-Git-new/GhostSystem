using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class GhostSystem : MonoBehaviour
{
    [Header("설정")]
    public Transform playerTransform;
    public GameObject ghostPrefab;    

    private string savePath;

    private List<GhostFrame> recordBuffer = new List<GhostFrame>();
    private bool isRecording = false;
    private float recordStartTime;

    private List<GhostFrame> replayBuffer = new List<GhostFrame>();
    private GameObject activeGhost;
    private bool isReplaying = false;
    private float replayStartTime;
    private int nextFrameIndex = 0;

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "ghost_record.bin");
    }


    public void StartRecording()
    {
        if (isReplaying) StopReplay();

        recordBuffer.Clear();
        recordStartTime = Time.time;
        isRecording = true;
        Debug.Log("<color=red>● 녹화 시작</color>");
    }

    void FixedUpdate()
    {
        if (isRecording && playerTransform != null)
        {
            float elapsed = Time.time - recordStartTime;
            recordBuffer.Add(new GhostFrame(elapsed, playerTransform.position, playerTransform.rotation));
        }
    }

    public void StopAndSave()
    {
        if (!isRecording) return;
        isRecording = false;

        SaveBinaryFile();
        Debug.Log("<color=white>■ 녹화 중지 및 저장 완료</color>");
    }

    private void SaveBinaryFile()
    {
        using (FileStream fs = new FileStream(savePath, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            bw.Write("GHOST");
            bw.Write(recordBuffer.Count);

            foreach (var frame in recordBuffer)
            {
                bw.Write(frame.timestamp);
                // Position (Vector3)
                bw.Write(frame.position.x); bw.Write(frame.position.y); bw.Write(frame.position.z);
                // Rotation (Quaternion)
                bw.Write(frame.rotation.x); bw.Write(frame.rotation.y); bw.Write(frame.rotation.z); bw.Write(frame.rotation.w);
            }
        }
    }


    public void StartReplay()
    {
        if (isRecording) isRecording = false;

        if (!File.Exists(savePath)) { Debug.LogError("저장된 고스트 파일이 없습니다!"); return; }

        LoadBinaryFile();

        if (replayBuffer.Count > 0)
        {
            if (activeGhost != null) Destroy(activeGhost);

            activeGhost = Instantiate(ghostPrefab, replayBuffer[0].position, replayBuffer[0].rotation);

            if (activeGhost.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;

            replayStartTime = Time.time;
            nextFrameIndex = 1;
            isReplaying = true;
            Debug.Log("<color=green>▶ 리플레이 재생 시작</color>");
        }
    }

    public void StopReplay()
    {
        isReplaying = false;
        if (activeGhost != null) Destroy(activeGhost);
        Debug.Log("리플레이 중지");
    }

    private void LoadBinaryFile()
    {
        replayBuffer.Clear();
        using (FileStream fs = new FileStream(savePath, FileMode.Open))
        using (BinaryReader br = new BinaryReader(fs))
        {
            try
            {
                string header = br.ReadString();
                if (header != "GHOST") return;

                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    float t = br.ReadSingle();
                    Vector3 pos = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    Quaternion rot = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    replayBuffer.Add(new GhostFrame(t, pos, rot));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("파일 읽기 오류: " + e.Message);
            }
        }
    }

    void Update()
    {
        if (isReplaying && activeGhost != null)
        {
            float elapsed = Time.time - replayStartTime;

            while (nextFrameIndex < replayBuffer.Count && elapsed > replayBuffer[nextFrameIndex].timestamp)
            {
                nextFrameIndex++;
            }

            if (nextFrameIndex < replayBuffer.Count)
            {
                GhostFrame prev = replayBuffer[nextFrameIndex - 1];
                GhostFrame next = replayBuffer[nextFrameIndex];

                float t = (elapsed - prev.timestamp) / (next.timestamp - prev.timestamp);
                activeGhost.transform.position = Vector3.Lerp(prev.position, next.position, t);
                activeGhost.transform.rotation = Quaternion.Slerp(prev.rotation, next.rotation, t);
            }
            else
            {
                StopReplay();
            }
        }
    }
}