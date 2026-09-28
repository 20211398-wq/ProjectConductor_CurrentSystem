using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OffScreenIndicator : MonoBehaviour
{
    [Header("화면 밖 표시기 세팅")]
    [Tooltip("화면에 표시될 화살표 UI 프리팹 (Canvas 아래에 생성됩니다)")]
    public GameObject indicatorPrefab;
    
    [Tooltip("화살표가 화면 테두리에서 얼마나 떨어져 있을지 여백 (픽셀 단위)")]
    public float screenMargin = 50f;

    private Camera mainCamera;
    private Dictionary<OverloadCurrent, GameObject> activeIndicators = new Dictionary<OverloadCurrent, GameObject>();

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null || indicatorPrefab == null) return;

        // 씬에 살아있는 모든 전류를 찾습니다. (자주 생성/파괴되지 않으므로 매 프레임 찾아도 큰 무리 없음)
        OverloadCurrent[] currents = FindObjectsOfType<OverloadCurrent>();

        // 죽었거나 없어진 전류의 인디케이터 삭제
        List<OverloadCurrent> keysToRemove = new List<OverloadCurrent>();
        foreach (var kvp in activeIndicators)
        {
            bool found = false;
            foreach (var c in currents)
            {
                if (c == kvp.Key) { found = true; break; }
            }
            if (!found) keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
        {
            if (activeIndicators[key] != null) Destroy(activeIndicators[key]);
            activeIndicators.Remove(key);
        }

        // 각 전류별로 화면 밖인지 검사
        foreach (var current in currents)
        {
            if (current == null) continue;

            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(current.transform.position);
            bool isOffScreen = false;

            // 카메라 뒤에 있거나, 화면 테두리를 벗어났는지 확인
            if (viewportPoint.z < 0 || viewportPoint.x < 0 || viewportPoint.x > 1 || viewportPoint.y < 0 || viewportPoint.y > 1)
            {
                isOffScreen = true;
            }

            if (isOffScreen)
            {
                // 화살표 생성 (없으면)
                if (!activeIndicators.ContainsKey(current))
                {
                    GameObject newInd = Instantiate(indicatorPrefab, transform); // 캔버스 자식으로
                    newInd.transform.localScale = Vector3.one; // 크기 버그 방지
                    
                    // 각 전류의 고유 색상(A=빨강, B=노랑 등)을 화살표에 입힘
                    Image img = newInd.GetComponentInChildren<Image>();
                    if (img != null)
                    {
                        if (current.type == OverloadCurrent.CurrentType.A) img.color = Color.red;
                        else if (current.type == OverloadCurrent.CurrentType.B) img.color = Color.yellow;
                        else if (current.type == OverloadCurrent.CurrentType.C) img.color = Color.cyan; // 파란색이 너무 어두울 수 있어서 하늘색으로
                    }
                    activeIndicators.Add(current, newInd);
                }

                GameObject indicator = activeIndicators[current];
                if (!indicator.activeSelf) indicator.SetActive(true);

                // 카메라 뒤에 있는 경우 화면 좌표 반전
                Vector3 screenPoint = mainCamera.WorldToScreenPoint(current.transform.position);
                if (viewportPoint.z < 0)
                {
                    screenPoint *= -1;
                }

                // 캔버스 크기에 맞춰 완벽하게 대응하기 위해 RectTransform 사용
                RectTransform canvasRect = GetComponent<RectTransform>();
                RectTransform indRect = indicator.GetComponent<RectTransform>();

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

                // 화면 테두리 제한 (캔버스 크기 기준)
                Vector2 canvasSize = canvasRect.rect.size;
                Vector2 bounds = (canvasSize / 2f) - new Vector2(screenMargin, screenMargin);

                // 0으로 나누기 방지
                if (localPoint.x == 0) localPoint.x = 0.0001f;
                float m = localPoint.y / localPoint.x;

                Vector3 clampedPoint = localPoint;

                // 1. 먼저 좌/우 테두리 중 어디에 부딪히는지 계산
                if (localPoint.x > 0)
                {
                    // 오른쪽 테두리
                    clampedPoint = new Vector3(bounds.x, m * bounds.x, 0);
                }
                else
                {
                    // 왼쪽 테두리
                    clampedPoint = new Vector3(-bounds.x, m * -bounds.x, 0);
                }

                // 2. 만약 위에서 계산한 Y좌표가 위/아래 테두리를 뚫고 나갔다면, 위/아래 테두리로 다시 고정
                if (clampedPoint.y > bounds.y)
                {
                    // 위쪽 테두리
                    clampedPoint = new Vector3(bounds.y / m, bounds.y, 0);
                }
                else if (clampedPoint.y < -bounds.y)
                {
                    // 아래쪽 테두리
                    clampedPoint = new Vector3(-bounds.y / m, -bounds.y, 0);
                }

                // UI 위치 적용
                indRect.anchoredPosition = clampedPoint;

                // 화살표 방향 회전 (전류 쪽을 가리키도록)
                float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
                // UI 화살표는 기본적으로 위쪽(Up)을 향하고 있다고 가정하므로 90도를 빼줍니다.
                indRect.localRotation = Quaternion.Euler(0, 0, angle - 90f);
            }
            else
            {
                // 화면 안에 들어오면 화살표 숨김
                if (activeIndicators.ContainsKey(current))
                {
                    if (activeIndicators[current].activeSelf)
                        activeIndicators[current].SetActive(false);
                }
            }
        }
    }
}
