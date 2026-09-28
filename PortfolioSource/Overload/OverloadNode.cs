using UnityEngine;
using System.Collections.Generic;

public class OverloadNode : MonoBehaviour
{
    [Header("상태 이미지 (Sprite)")]
    public Sprite normalSprite;
    public Sprite brokenSprite;

    [Header("현재 상태 (디버그용)")]
    public bool isBroken = false;
    public bool HasPlayer => playersInNode.Count > 0; // 플레이어 존재 여부 (속성으로 변경)

    // 현재 노드 내부에 존재하는 전류들의 '오브젝트'를 직접 저장
    private List<OverloadCurrent> currentsInNode = new List<OverloadCurrent>();
    
    // [버그 픽스] 명단 관리 방식으로 변경 (2명 동시 인식)
    private HashSet<PlayerShockHandler> playersInNode = new HashSet<PlayerShockHandler>();
    
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // 디자이너가 처음부터 isBroken을 체크했다면 부서진 이미지를 띄움
            spriteRenderer.sprite = isBroken ? brokenSprite : normalSprite; 
        }
    }

    void Update()
    {
        if (!isBroken)
        {
            bool hasA = false, hasB = false, hasC = false;
            foreach (var c in currentsInNode)
            {
                if (c == null) continue;
                if (c.type == OverloadCurrent.CurrentType.A) hasA = true;
                if (c.type == OverloadCurrent.CurrentType.B) hasB = true;
                if (c.type == OverloadCurrent.CurrentType.C) hasC = true;
            }

            if (hasA && hasB && hasC)
            {
                if (HasPlayer)
                {
                    // ⭐️ 기획자님 의도: 플레이어가 서 있으면 게임 오버가 터지지 않고, 플레이어가 몸으로 때움 (감전)
                    foreach (var p in playersInNode)
                    {
                        if (p != null) p.TriggerShock();
                    }
                }
                else
                {
                    TriggerGameOver();
                }
            }
        }
    }

    private void TriggerGameOver()
    {
        Debug.Log($"<color=red>🔥🔥 [과부하 발생] 3개의 전류가 만나 게임 오버!! 🔥🔥</color>");
        
        // 1. 모든 움직임 일시정지 (시간 멈춤)
        Time.timeScale = 0f;

        // 2. TODO: UI 담당자님이 나중에 아래 주석을 풀고 UI 호출 코드를 넣으시면 됩니다!
        // UIManager.instance.ShowGameOverUI();
    }

    // 디버거(F1) 등 외부에서 강제로 갭존을 부술 때 쓰는 함수 (유지)
    public void TriggerOverload()
    {
        isBroken = true;
        if (spriteRenderer != null && brokenSprite != null)
        {
            spriteRenderer.sprite = brokenSprite;
        }
        Debug.Log($"<color=red>[OverloadNode]</color> 과부하 발생! 단선되며 내부 전류가 폭발합니다!");

        if (!HasPlayer)
        {
            foreach (var current in new List<OverloadCurrent>(currentsInNode))
            {
                if (current != null) current.KillCurrent();
            }
            currentsInNode.Clear();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 플레이어 감지 (PlayerShockHandler 스크립트 존재 여부로 확인)
        PlayerShockHandler shockHandler = other.GetComponent<PlayerShockHandler>();
        if (shockHandler != null)
        {
            playersInNode.Add(shockHandler);
            Debug.Log($"<color=cyan>[OverloadNode]</color> 플레이어가 갭존을 밟았습니다! 현재 인원: {playersInNode.Count}명");
            return;
        }

        // 2. 전류 감지
        OverloadCurrent current = other.GetComponent<OverloadCurrent>();
        if (current != null)
        {
            if (!currentsInNode.Contains(current)) 
            {
                currentsInNode.Add(current);
                if (!isBroken) 
                {
                    Debug.Log($"<color=white>[Overload 센서]</color> {gameObject.name}에 {current.type} 전류 진입! (현재 모인 개수: {currentsInNode.Count}/3)");
                }
            }
            
            if (isBroken)
            {
                if (!HasPlayer)
                {
                    Debug.Log($"<color=orange>[OverloadNode]</color> 플레이어 부재! {current.type} 파괴!");
                    current.KillCurrent(); 
                    currentsInNode.Remove(current);
                }
                else
                {
                    Debug.Log($"<color=green>[OverloadNode]</color> 플레이어가 밟고 있음! {current.type} 무사 통과!");
                    // ⭐️ 전류가 무사 통과할 때, 플레이어는 감전(패널티) 당함!
                    foreach (var p in playersInNode)
                    {
                        if (p != null) p.TriggerShock();
                    }
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        OverloadCurrent current = other.GetComponent<OverloadCurrent>();
        if (current != null)
        {
            currentsInNode.Remove(current);
        }
        
        PlayerShockHandler shockHandler = other.GetComponent<PlayerShockHandler>();
        if (shockHandler != null)
        {
            playersInNode.Remove(shockHandler);
            Debug.Log($"<color=gray>[OverloadNode]</color> 플레이어가 갭존에서 벗어났습니다. 남은 인원: {playersInNode.Count}명");
        }
    }
}
