namespace ProjectConductor
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;

    public class SourceNode : MonoBehaviour
    {
        [Header("Type A (빠름)")]
        public WirePath pathA;
        public float speedA = 200f;
        public Color colorA = Color.red;

        [Header("Type B (보통)")]
        public WirePath pathB;
        public float speedB = 150f;
        public Color colorB = Color.yellow;

        [Header("Type C (느림)")]
        public WirePath pathC;
        public float speedC = 100f;
        public Color colorC = Color.blue;

        [Header("기본 설정")]
        public GameObject currentPrefab;
        public Image cooldownUI;
        public UnityEvent onReachedTarget; // 3개 전부 도달 시 실행할 이벤트

        private bool isSpawning = false;
        private bool isCurrentActive = false;
        
        // 맵에 살아있는 전류들을 추적하기 위한 리스트
        private System.Collections.Generic.List<CurrentObject> spawnedCurrents = new System.Collections.Generic.List<CurrentObject>();

        void Start() 
        { 
            if (cooldownUI != null) cooldownUI.gameObject.SetActive(false); 
        }

        public void TriggerSpawn()
        {
            if (!gameObject.scene.IsValid() || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"<color=red>[경고]</color> 스위치에 연결된 SourceNode가 씬 오브젝트가 아니거나 꺼져있습니다! (이름: {gameObject.name})");
                return;
            }

            if (isSpawning || isCurrentActive) return;
            StartCoroutine(SpawnSequence());
        }

        private System.Collections.IEnumerator SpawnSequence()
        {
            isSpawning = true;
            float timer = 3f;
            Debug.Log("<color=cyan>[SourceNode]</color> 스위치 입력 확인! 3초 카운트다운 시작...");

            if (cooldownUI != null) cooldownUI.gameObject.SetActive(true);

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                if (cooldownUI != null) 
                    cooldownUI.fillAmount = timer / 3f;
                yield return null;
            }

            if (cooldownUI != null) cooldownUI.gameObject.SetActive(false);

            isSpawning = false;
            Debug.Log("<color=cyan>[SourceNode]</color> 3초 경과! 3종 전류 발사 시도!");
            SpawnCurrent();
        }

        private void SpawnCurrent()
        {
            if (currentPrefab == null)
            {
                Debug.LogError("<color=red>[에러]</color> SourceNode에 Current Prefab이 할당되지 않았습니다!");
                return;
            }

            isCurrentActive = true;
            spawnedCurrents.Clear();
            bool anySpawned = false;

            // A, B, C 각각 연결되어 있다면 스폰
            if (pathA != null) anySpawned |= TrySpawnSingle(pathA, speedA, colorA);
            if (pathB != null) anySpawned |= TrySpawnSingle(pathB, speedB, colorB);
            if (pathC != null) anySpawned |= TrySpawnSingle(pathC, speedC, colorC);

            if (!anySpawned)
            {
                Debug.LogError("<color=red>[에러]</color> 연결된 Wire Path가 하나도 없습니다!");
                isCurrentActive = false;
            }
        }

        private bool TrySpawnSingle(WirePath path, float speed, Color col)
        {
            try 
            {
                path.CalculatePath(); 
                GameObject newObj = Instantiate(currentPrefab, path.GetPositionAtT(0), Quaternion.identity);
                CurrentObject currentObj = newObj.GetComponent<CurrentObject>();

                if (currentObj != null)
                {
                    currentObj.Initialize(path, this, speed, col);
                    spawnedCurrents.Add(currentObj);
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"<color=red>[에러]</color> 전류 생성 중 문제 발생! Wire Path 설정 확인! 상세: {e.Message}");
            }
            return false;
        }

        // 전류 중 하나라도 파괴(실패)되면 호출되는 함수
        public void OnCurrentFailed(CurrentObject failedObj)
        {
            Debug.Log("<color=red>[SourceNode]</color> 단선 발생! 살아있는 모든 전류를 파괴합니다!");
            foreach (var c in spawnedCurrents)
            {
                if (c != null && c.gameObject != null) Destroy(c.gameObject);
            }
            spawnedCurrents.Clear();
            ResetSpawner();
        }

        // 전류가 목표(Sink)에 하나 도착할 때마다 호출되는 함수
        public void OnCurrentReachedTarget(CurrentObject reachedObj)
        {
            spawnedCurrents.Remove(reachedObj);
            Destroy(reachedObj.gameObject);

            // 살아있는 전류가 하나도 없고 스포너가 활성 상태라면 -> 올 클리어!
            if (spawnedCurrents.Count == 0 && isCurrentActive)
            {
                Debug.Log("<color=green>[SourceNode]</color> 모든 전류가 목표에 도달했습니다! 클리어 이벤트 발생!");
                onReachedTarget?.Invoke();
                ResetSpawner();
            }
        }

        public void ResetSpawner() 
        { 
            isCurrentActive = false; 
        }
    }
}