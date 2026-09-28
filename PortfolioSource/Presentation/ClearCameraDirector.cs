using UnityEngine;
using UnityEngine.Tilemaps; // [추가] 타일맵 사용을 위함
using System.Collections;
using ProjectConductor; // PlayerController namespace

public class ClearCameraDirector : MonoBehaviour
{
    [Header("카메라 수동 연출 세팅")]
    [Tooltip("비출 대상 (문 오브젝트)")]
    public Transform targetDoor;
    
    [Tooltip("줌인 되었을 때의 카메라 렌즈 크기 (기본 약 5)")]
    public float targetZoomSize = 5f;

    [Tooltip("카메라가 문 쪽으로 이동하는 데 걸리는 시간 (초)")]
    public float moveDuration = 2.0f;

    [Tooltip("다 이동한 후 문을 비추고 대기하는 시간 (초)")]
    public float waitDuration = 1.0f;

    public void PlayZoom()
    {
        StartCoroutine(ZoomRoutine());
    }

    private IEnumerator ZoomRoutine()
    {
        Debug.Log($"<color=green>[CameraDirector]</color> 문 개방 연출 시작! 플레이어 조작을 잠그고 카메라를 이동합니다.");

        // 1. 플레이어 조작 잠금
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            player.enabled = false;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
        }

        // 2. 메인 카메라 제어권 뺏기 (CoopCameraControl 비활성화)
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        CoopCameraControl coopCam = mainCam.GetComponent<CoopCameraControl>();
        if (coopCam != null)
        {
            coopCam.enabled = false;
        }

        // 3. 현재 카메라 위치 및 줌 사이즈 저장
        Vector3 startPos = mainCam.transform.position;
        float startSize = mainCam.orthographicSize;

        // 목표 위치 계산 (Z축은 유지)
        Vector3 endPos = targetDoor != null ? targetDoor.position : startPos;
        endPos.z = startPos.z;

        // [추가] 레벨 디자이너의 빈 공간 방지 요청: 기존 CoopCameraControl의 맵 제한 수식을 훔쳐 와서 endPos를 제한(Clamp)함
        if (coopCam != null && coopCam.useBounds && coopCam.targetTilemap != null)
        {
            Tilemap tilemap = coopCam.targetTilemap;
            tilemap.CompressBounds();
            Bounds bounds = tilemap.localBounds;

            Vector2 minBounds = tilemap.transform.TransformPoint(bounds.min);
            Vector2 maxBounds = tilemap.transform.TransformPoint(bounds.max);

            float camHalfHeight = targetZoomSize;
            float camHalfWidth = targetZoomSize * mainCam.aspect;

            float clampedX = Mathf.Clamp(endPos.x, minBounds.x + camHalfWidth, maxBounds.x - camHalfWidth);
            float clampedY = Mathf.Clamp(endPos.y, minBounds.y + camHalfHeight, maxBounds.y - camHalfHeight);

            endPos = new Vector3(clampedX, clampedY, endPos.z);
            Debug.Log($"<color=yellow>[CameraDirector]</color> 빈 공간 렌더링 방지! 목표 위치를 타일맵 내부로 제한합니다.");
        }

        // 4. 스르륵 이동하기 (Lerp)
        float timer = 0f;
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration); // 부드러운 가감속

            mainCam.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCam.orthographicSize = Mathf.Lerp(startSize, targetZoomSize, t);
            
            yield return null;
        }

        // 정확한 목표치 고정
        mainCam.transform.position = endPos;
        mainCam.orthographicSize = targetZoomSize;

        // 5. 문 비추면서 대기
        yield return new WaitForSeconds(waitDuration);

        // 6. 메인 카메라 제어권 돌려주기 (CoopCameraControl 켜기)
        // 켜기만 하면 CoopCameraControl의 SmoothDamp 덕분에 알아서 플레이어 쪽으로 자연스럽게 돌아옵니다!
        if (coopCam != null)
        {
            coopCam.enabled = true;
        }

        // 카메라 복귀 대기 (약 1.5초)
        yield return new WaitForSeconds(1.5f);

        // 7. 플레이어 조작 복구
        foreach (var player in players)
        {
            player.enabled = true;
        }

        Debug.Log($"<color=green>[CameraDirector]</color> 카메라 연출 종료. 조작이 복구되었습니다.");
    }
}
