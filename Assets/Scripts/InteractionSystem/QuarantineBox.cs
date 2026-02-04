using UnityEngine;
using System.Collections;
using DG.Tweening; // <-- Обязательно подключи DOTween!
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class QuarantineBox : SnapZone
{
    [Header("Карантинный ящик")]
    [SerializeField] private float destroyDelay = 1.5f;

    [Header("Анимация крышки")]
    [SerializeField] private Transform lidTransform;           // Ссылка на крышку (дочерний объект)
    [SerializeField] private float lidOpenAngle = -50f;         // На сколько градусов открываем (по Y)
    [SerializeField] private float lidAnimationDuration = 0.25f; // Длительность одной фазы анимации
    [SerializeField] private Ease lidEase = Ease.OutBack;       // Тип анимации (можно поиграться)

    private Quaternion _initialLidRotation;

    private void Awake()
    {
        // Сохраняем начальный поворот крышки
        if (lidTransform != null)
            _initialLidRotation = lidTransform.localRotation;
    }

    private void OnEnable()
    {
        onItemSnapped.AddListener(OnMineralQuarantined);
    }

    private void OnDisable()
    {
        onItemSnapped.RemoveListener(OnMineralQuarantined);
    }

    public override bool CanSnap(GrabbableItem item)
    {
        if (!base.CanSnap(item)) return false;
        var mineralData = item.GetComponentInChildren<MineralData>();
        return mineralData != null && mineralData.isAnomaly;
    }

    public override void Snap(GrabbableItem item)
    {
        if (!CanSnap(item)) return;
        base.Snap(item);
    }

    public override void OnItemGrabbedFromZone(GrabbableItem grabbedItem)
    {
        // Запрещаем вытаскивать из карантина
        FindObjectOfType<CanGrab>()?.ForceRelease();
    }

    private void OnMineralQuarantined(GrabbableItem item)
    {
        // Запускаем анимацию крышки + уничтожение предмета
        AnimateLidAndDestroy(item.gameObject);
    }

    private void AnimateLidAndDestroy(GameObject obj)
    {
        if (lidTransform == null)
        {
            // Если крышка не назначена — просто уничтожаем с задержкой как раньше
            //StartCoroutine(DestroyAfterDelay(obj));
            return;
        }

        // Последовательность: открываем → ждём немного → закрываем → уничтожаем предмет
        var sequence = DOTween.Sequence();

        // 1. Открываем крышку
        sequence.Append(
            lidTransform
                .DOLocalRotate(new Vector3(0, lidOpenAngle, 0), lidAnimationDuration)
                .SetEase(lidEase)
        );

        // 2. Небольшая пауза на "максимальном открытии" (по желанию)
        sequence.AppendInterval(0.15f);

        // 3. Закрываем обратно
        sequence.Append(
            lidTransform
                .DOLocalRotate(_initialLidRotation.eulerAngles, lidAnimationDuration)
                .SetEase(Ease.InOutQuad)
        );

        // 4. После полного закрытия — уничтожаем предмет
        sequence.AppendInterval(destroyDelay - (lidAnimationDuration * 2 + 0.15f)); // подгоняем под общую задержку

        sequence.OnComplete(() =>
        {
            obj.SetActive(false);
        });

        sequence.Play();
    }

    // Альтернативный вариант через корутину (если не любишь Sequence)
    /*
    private IEnumerator AnimateLidAndDestroyCoroutine(GameObject obj)
    {
        if (lidTransform != null)
        {
            // Открываем
            lidTransform.DOLocalRotate(new Vector3(0, lidOpenAngle, 0), lidAnimationDuration)
                        .SetEase(lidEase);

            yield return new WaitForSeconds(lidAnimationDuration + 0.15f);

            // Закрываем
            lidTransform.DOLocalRotate(_initialLidRotation.eulerAngles, lidAnimationDuration)
                        .SetEase(Ease.InOutQuad);

            yield return new WaitForSeconds(lidAnimationDuration);
        }

        yield return new WaitForSeconds(destroyDelay - (lidAnimationDuration * 2 + 0.15f));
        obj.SetActive(false);
    }
    */

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.5f, new Vector3(1.2f, 1f, 1.2f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(1.3f, 1.1f, 1.3f));
    }

    // Для удобства в редакторе — сбрасываем поворот крышки при валидации
    private void OnValidate()
    {
        if (lidTransform != null && Application.isPlaying == false)
        {
            lidTransform.localRotation = Quaternion.Euler(0, 0, 0); // или твоё начальное положение
        }
    }
}