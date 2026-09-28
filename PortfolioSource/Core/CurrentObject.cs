using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CurrentObject : MonoBehaviour
{
    private WirePath myWire;
    private ProjectConductor.SourceNode mySource;
    private float speed;
    private float t = 0f;

    public LayerMask gapLayer;

    private Vector3 prevPos;
    private HashSet<GapZone> processedGaps = new HashSet<GapZone>();

    public void Initialize(WirePath wire, ProjectConductor.SourceNode source, float moveSpeed, Color col)
    {
        myWire = wire;
        mySource = source;
        speed = moveSpeed;
        t = 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = col;

        if (myWire.nodes.Length > 0)
        {
            transform.position = myWire.nodes[0].position;
            prevPos = transform.position;
        }
    }

    void Update()
    {
        if (myWire == null || myWire.nodes.Length < 2) return;

        t += (speed / myWire.TotalLength) * Time.deltaTime;
        Vector3 nextPos = myWire.GetPositionAtT(t);

        // GapZone 판정 로직 - 고속 이동 시 건너뛰는 문제 (Linecast) 및 중복 감전 방지
        RaycastHit2D[] hits = Physics2D.LinecastAll(prevPos, nextPos, gapLayer);
        foreach (var hit in hits)
        {
            GapZone gap = hit.collider.GetComponent<GapZone>();
            if (gap != null && !processedGaps.Contains(gap))
            {
                processedGaps.Add(gap);
                if (!gap.isBridged) // 플레이어가 없으면 실패
                {
                    mySource.OnCurrentFailed(this); // 실패 시 스포너에 보고 (나머지 전류도 파괴됨)
                    return;
                }
                else
                {
                    // HTML 기획과 동일하게, 연결 시 전류는 계속 가고 플레이어는 감전 (800ms)
                    gap.ShockPlayers(0.8f);
                }
            }
        }

        // 아주 근접하게 시작된 경우를 위한 안전 장치
        Collider2D overlapHit = Physics2D.OverlapPoint(nextPos, gapLayer);
        if (overlapHit != null)
        {
            GapZone gap = overlapHit.GetComponent<GapZone>();
            if (gap != null && !processedGaps.Contains(gap))
            {
                processedGaps.Add(gap);
                if (!gap.isBridged)
                {
                    mySource.OnCurrentFailed(this);
                    return;
                }
                else
                {
                    gap.ShockPlayers(0.8f);
                }
            }
        }

        transform.position = nextPos;
        prevPos = nextPos;
        myWire.UpdateActiveLine(t);

        // Sink(목적지) 도달 시 [cite: 61, 62]
        if (t >= 1f)
        {
            mySource.OnCurrentReachedTarget(this); // 개별 도착 보고
        }
    }
}