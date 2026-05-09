using UnityEngine;

public class HeroKnightAnimationEvents : MonoBehaviour
{
    [SerializeField] private GameObject slideDustPrefab;

    // Hero Knight 원본 애니메이션 이벤트가 호출하는 함수다. 데모 스크립트를 제거했기 때문에 여기서 먼지 이펙트만 받는다.
    private void AE_SlideDust()
    {
        if (slideDustPrefab == null)
            return;

        Vector3 spawnPosition = transform.position + Vector3.down * 0.45f;
        Instantiate(slideDustPrefab, spawnPosition, transform.rotation);
    }
}
