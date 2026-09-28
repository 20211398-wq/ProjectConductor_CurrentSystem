using UnityEngine;
using System.Collections.Generic;

public class OverloadDebugger : MonoBehaviour
{
    [Header("단선 강제 유발 디버그 키")]
    public KeyCode debugKey = KeyCode.F1;

    void Update()
    {
        // 디버그 키(기본 F1)를 눌렀을 때
        if (Input.GetKeyDown(debugKey))
        {
            ForceBreakClosestNode();
        }
    }

    private void ForceBreakClosestNode()
    {
        // 1. 플레이어 오브젝트 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[Debugger] Player 태그를 가진 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 2. 씬에 깔려있는 모든 OverloadNode 싹 다 긁어오기
        OverloadNode[] allNodes = FindObjectsOfType<OverloadNode>();
        if (allNodes == null || allNodes.Length == 0)
        {
            Debug.LogWarning("[Debugger] 씬에 자동 생성된 OverloadNode(갭존)가 하나도 없습니다!");
            return;
        }

        OverloadNode closestNode = null;
        float minDistance = float.MaxValue;

        // 3. 거리 비교를 통해 플레이어와 '가장 가까운' 놈 하나만 골라내기
        foreach (OverloadNode node in allNodes)
        {
            // 이미 부서져있는 놈은 터뜨릴 필요 없으니 제외
            if (node.isBroken) continue; 

            float dist = Vector3.Distance(player.transform.position, node.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestNode = node;
            }
        }

        // 4. 제일 가까운 놈 강제로 터뜨리기!
        if (closestNode != null)
        {
            Debug.Log($"<color=yellow>[Debugger]</color> 플레이어 코앞에 있는 갭존({closestNode.gameObject.name}, 거리: {minDistance:F2})을 강제로 폭발시킵니다!");
            closestNode.TriggerOverload(); // 단선 트리거!
        }
        else
        {
            Debug.Log($"<color=yellow>[Debugger]</color> 폭발시킬 수 있는 멀쩡한 갭존이 씬에 더 이상 없습니다.");
        }
    }
}
