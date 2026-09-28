using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AutoSourceNode : MonoBehaviour
{
    [Header("프리팹 설정")]
    public GameObject currentPrefab;     // OverloadCurrent 스크립트가 달린 새로운 전류 프리팹
    public GameObject overloadNodePrefab;// OverloadNode 스크립트가 달린 갭존 프리팹

    [Header("Type A")]
    public WirePath pathA;
    public float speedA = 200f;
    public Color colorA = Color.red;
    public Sprite spriteA; // A 전류 앞 대가리 이미지

    [Header("Type B")]
    public WirePath pathB;
    public float speedB = 150f;
    public Color colorB = Color.yellow;
    public Sprite spriteB; // B 전류 앞 대가리 이미지

    [Header("Type C")]
    public WirePath pathC;
    public float speedC = 100f;
    public Color colorC = Color.blue;
    public Sprite spriteC; // C 전류 앞 대가리 이미지

    [Header("과부하 감지기 설정")]
    public float overloadDetectorSize = 2.5f; // 교차점 센서의 충돌체 크기 (크게 할수록 타이밍이 안 맞아도 잘 겹침)

    [Header("재생성 설정")]
    public float respawnDelay = 1.5f;

    void Start()
    {
        // 1. 유저 아이디어: A,B,C 교차점을 찾아내서 자동으로 GapZone(OverloadNode) 깔아버리기!
        DetectAndSpawnOverloadNodes();

        // 2. 게임 시작 시 1.5초 대기 후 스위치 없이 3가닥 동시 자동 출발
        StartCoroutine(InitialSpawnRoutine());
    }

    private void DetectAndSpawnOverloadNodes()
    {
        if (pathA == null || pathB == null || pathC == null || overloadNodePrefab == null) 
        {
            Debug.LogWarning("[AutoSourceNode] 경로 또는 프리팹이 빈칸이라 GapZone을 생성할 수 없습니다.");
            return;
        }

        List<Vector3> nodesA = GetPathPoints(pathA);
        List<Vector3> nodesB = GetPathPoints(pathB);
        List<Vector3> nodesC = GetPathPoints(pathC);

        List<Vector3> intersections = new List<Vector3>();

        int spawnCount = 0;
        foreach (Vector3 pA in nodesA)
        {
            bool matchB = false;
            foreach (Vector3 pB in nodesB) { if (Vector3.Distance(pA, pB) < 0.1f) { matchB = true; break; } }
            
            bool matchC = false;
            foreach (Vector3 pC in nodesC) { if (Vector3.Distance(pA, pC) < 0.1f) { matchC = true; break; } }

            if (matchB && matchC)
            {
                bool alreadyFound = false;
                foreach(Vector3 found in intersections) { if (Vector3.Distance(pA, found) < 0.1f) { alreadyFound = true; break; } }
                
                if (!alreadyFound)
                {
                    intersections.Add(pA);

                    // 핵심 픽스: 기획자님이 이 교차점에 수동으로 갭존을 깔아두셨는지 검사!
                    bool hasManualGapZone = false;
                    OverloadNode[] existingNodes = FindObjectsOfType<OverloadNode>();
                    foreach (var existing in existingNodes)
                    {
                        if (Vector3.Distance(existing.transform.position, pA) < 0.5f)
                        {
                            hasManualGapZone = true;
                            Debug.Log($"<color=cyan>[AutoSourceNode]</color> 교차점({pA})에 기획자님이 설치한 수동 갭존이 이미 있습니다! 자동 센서를 겹쳐 깔지 않습니다.");
                            break;
                        }
                    }

                    // 수동 갭존이 없는 순수 교차점에만 자동 센서를 깝니다.
                    if (!hasManualGapZone)
                    {
                        spawnCount++;
                        Debug.Log($"<color=magenta>[AutoSourceNode]</color> 겹치는 교차점 발견! 자동 과부하 센서를 생성합니다: {pA}");
                        GameObject newNode = Instantiate(overloadNodePrefab, pA, Quaternion.identity);
                        newNode.name = "Auto_OverloadZone_" + spawnCount;

                    // 핵심 해결책: 자동 생성된 녀석은 무조건 '투명 감지기'여야 하므로, 
                    // 기획자가 프리팹에서 실수로 isBroken을 켜놨더라도 강제로 꺼버립니다!
                    OverloadNode nodeScript = newNode.GetComponent<OverloadNode>();
                    if (nodeScript != null)
                    {
                        nodeScript.isBroken = false;
                        
                        // 추가 해결책: 수동 갭존은 기획자가 아주 작게 설정해 뒀더라도,
                        // 전류 3개가 겹치는지 감지해야 하는 '투명 센서'는 범위를 강제로 넓혀줍니다!
                        BoxCollider2D boxCol = newNode.GetComponent<BoxCollider2D>();
                        if (boxCol != null) boxCol.size = new Vector2(overloadDetectorSize, overloadDetectorSize);
                        
                        CircleCollider2D circleCol = newNode.GetComponent<CircleCollider2D>();
                        if (circleCol != null) circleCol.radius = overloadDetectorSize / 2f;
                    }
                    }
                }
            }
        } // foreach pA 닫기

        if (intersections.Count == 0)
        {
            Debug.LogWarning($"<color=yellow>[AutoSourceNode]</color> 세 전선이 겹치는 점을 하나도 찾지 못했습니다! 씬 뷰에서 점들이 진짜로 포개져 있는지 확인하세요.");
        }
    }

    private List<Vector3> GetPathPoints(WirePath path)
    {
        List<Vector3> points = new List<Vector3>();
        if (path != null && path.nodes != null)
        {
            foreach (Transform t in path.nodes)
            {
                if (t != null) points.Add(t.position); 
            }
        }
        return points;
    }

    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnSingle(OverloadCurrent.CurrentType.A);
        SpawnSingle(OverloadCurrent.CurrentType.B);
        SpawnSingle(OverloadCurrent.CurrentType.C);
    }

    // 특정 전류가 죽었을 때 해당 타입만 다시 소환
    public void RespawnCurrent(OverloadCurrent.CurrentType type)
    {
        StartCoroutine(RespawnRoutine(type));
    }

    private IEnumerator RespawnRoutine(OverloadCurrent.CurrentType type)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnSingle(type);
    }

    private void SpawnSingle(OverloadCurrent.CurrentType type)
    {
        WirePath targetPath = null;
        float targetSpeed = 0f;
        Color targetCol = Color.white;
        Sprite targetSprite = null;

        if (type == OverloadCurrent.CurrentType.A) { targetPath = pathA; targetSpeed = speedA; targetCol = colorA; targetSprite = spriteA; }
        else if (type == OverloadCurrent.CurrentType.B) { targetPath = pathB; targetSpeed = speedB; targetCol = colorB; targetSprite = spriteB; }
        else if (type == OverloadCurrent.CurrentType.C) { targetPath = pathC; targetSpeed = speedC; targetCol = colorC; targetSprite = spriteC; }

        if (targetPath != null && currentPrefab != null)
        {
            targetPath.CalculatePath();
            GameObject newObj = Instantiate(currentPrefab, targetPath.GetPositionAtT(0), Quaternion.identity);
            OverloadCurrent curr = newObj.GetComponent<OverloadCurrent>();
            if (curr != null)
            {
                curr.Initialize(type, targetPath, this, targetSpeed, targetCol, targetSprite);
            }
            else
            {
                Debug.LogError("[에러] 전류 프리팹에 OverloadCurrent 스크립트가 안 달려있습니다!");
            }
        }
    }

    // 에디터 화면에서 3선 교차점 미리보기 홀로그램 생성
    void OnDrawGizmos()
    {
        if (pathA == null || pathB == null || pathC == null) return;

        List<Vector3> nodesA = GetPathPoints(pathA);
        List<Vector3> nodesB = GetPathPoints(pathB);
        List<Vector3> nodesC = GetPathPoints(pathC);

        foreach (Vector3 pA in nodesA)
        {
            bool matchB = false;
            foreach (Vector3 pB in nodesB) { if (Vector3.Distance(pA, pB) < 0.1f) { matchB = true; break; } }
            
            bool matchC = false;
            foreach (Vector3 pC in nodesC) { if (Vector3.Distance(pA, pC) < 0.1f) { matchC = true; break; } }

            if (matchB && matchC)
            {
                // 겹치는 곳에 반투명 핑크색 구체 그리기 (크기 0.5)
                Gizmos.color = new Color(1f, 0f, 1f, 0.7f);
                Gizmos.DrawSphere(pA, 0.5f);
            }
        }
    }
}
