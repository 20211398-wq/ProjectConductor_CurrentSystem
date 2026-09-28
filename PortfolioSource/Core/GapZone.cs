using UnityEngine;
using System.Collections.Generic;

public class GapZone : MonoBehaviour
{
    public bool isBridged = false;
    private SpriteRenderer spriteRenderer;

    // 현재 영역 안에 있는 플레이어 목록을 추적 (P1, P2 대응)
    private List<PlayerWireInteractable> playersInZone = new List<PlayerWireInteractable>();

    // 기획서 11.2 규격 색상 
    public Color disconnectedColor = Color.red;
    public Color bridgedColor = Color.black;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisual();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 태그가 Player1 또는 Player2인 경우 목록에 추가
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            PlayerWireInteractable pwi = other.GetComponent<PlayerWireInteractable>();
            if (pwi != null && !playersInZone.Contains(pwi))
            {
                playersInZone.Add(pwi);
                UpdateBridgeStatus();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player1") || other.CompareTag("Player2"))
        {
            PlayerWireInteractable pwi = other.GetComponent<PlayerWireInteractable>();
            if (pwi != null && playersInZone.Contains(pwi))
            {
                playersInZone.Remove(pwi);
                UpdateBridgeStatus();
            }
        }
    }

    private void UpdateBridgeStatus()
    {
        // 한 명이라도 밟고 있으면 연결 상태(isBridged)가 됨 [cite: 45]
        isBridged = (playersInZone.Count > 0);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer != null)
        {
            // 플레이어가 있으면 검은색, 없으면 빨간색 
            spriteRenderer.color = isBridged ? bridgedColor : disconnectedColor;
        }
    }

    // HTML 프로토타입처럼 플레이어에게 감전 효과 전달 (0.8초)
    public void ShockPlayers(float duration = 0.8f)
    {
        // 도중에 플레이어가 역참조될 수 있으므로 역순 순회
        for (int i = playersInZone.Count - 1; i >= 0; i--)
        {
            if (playersInZone[i] != null)
            {
                playersInZone[i].ApplyShock(duration);
            }
            else
            {
                playersInZone.RemoveAt(i);
            }
        }
    }
}