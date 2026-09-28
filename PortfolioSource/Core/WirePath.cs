using UnityEngine;

public class WirePath : MonoBehaviour
{
    public Transform[] nodes;
    public LineRenderer backgroundLine;
    public LineRenderer activeLine;

    public float TotalLength { get; private set; }
    private float[] segmentLengths;

    void Awake()
    {
        // SetupLines 안에서 길이 계산도 하므로 굳이 두 번 부를 필요가 없습니다.
        SetupLines();
    }

    public void CalculatePath()
    {
        SetupLines(); // 외부에서 길이를 다시 계산하라고 할 때 사용
    }

    void SetupLines()
    {
        if (nodes == null || nodes.Length < 2) return;

        segmentLengths = new float[nodes.Length - 1];
        TotalLength = 0f;

        if (backgroundLine != null) backgroundLine.positionCount = nodes.Length;
        if (activeLine != null) activeLine.positionCount = 0;

        for (int i = 0; i < nodes.Length - 1; i++)
        {
            float dist = Vector2.Distance(nodes[i].position, nodes[i + 1].position);
            segmentLengths[i] = dist;
            TotalLength += dist;
            if (backgroundLine != null) backgroundLine.SetPosition(i, nodes[i].position);
        }
        if (backgroundLine != null) backgroundLine.SetPosition(nodes.Length - 1, nodes[nodes.Length - 1].position);
    }

    public void UpdateActiveLine(float t)
    {
        if (activeLine == null || nodes == null || nodes.Length < 2) return;

        // [수정] 진행도를 0~1 사이로 엄격하게 제한하여 마지막 구간 생략 방지
        t = Mathf.Clamp01(t);
        float targetLen = t * TotalLength;
        float currentLen = 0f;

        activeLine.positionCount = 0;
        for (int i = 0; i < nodes.Length - 1; i++)
        {
            // 현재 구간의 시작 노드 추가
            activeLine.positionCount++;
            activeLine.SetPosition(activeLine.positionCount - 1, nodes[i].position);

            // 현재 구간(Segment) 내에 목표 지점이 있는지 확인
            if (targetLen <= currentLen + segmentLengths[i] + 0.001f) // 미세 오차 허용
            {
                float segmentT = (segmentLengths[i] > 0) ? (targetLen - currentLen) / segmentLengths[i] : 1f;
                Vector3 endPos = Vector3.Lerp(nodes[i].position, nodes[i + 1].position, Mathf.Clamp01(segmentT));

                activeLine.positionCount++;
                activeLine.SetPosition(activeLine.positionCount - 1, endPos);
                return; // 목표 지점까지 그렸으므로 함수 종료
            }
            currentLen += segmentLengths[i];
        }
    }

    public Vector3 GetPositionAtT(float t)
    {
        if (nodes == null || nodes.Length < 2) return transform.position;

        t = Mathf.Clamp01(t); // 0~1 사이로 고정
        float targetLen = t * TotalLength;
        float currentLen = 0f;

        for (int i = 0; i < nodes.Length - 1; i++)
        {
            float segLen = Vector2.Distance(nodes[i].position, nodes[i + 1].position);

            // 내가 찾는 위치가 현재 구간(Segment) 안에 있는지 확인
            if (targetLen <= currentLen + segLen)
            {
                float segmentT = (targetLen - currentLen) / segLen;
                // 구간 내에서 선형 보간(Lerp) 수행
                return Vector3.Lerp(nodes[i].position, nodes[i + 1].position, segmentT);
            }
            currentLen += segLen;
        }
        return nodes[nodes.Length - 1].position;
    }
}