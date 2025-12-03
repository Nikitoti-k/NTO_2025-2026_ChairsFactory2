using UnityEngine;
using System;
using UnityEngine.Events; // если захочешь событие в инспекторе

namespace WrightAngle.Waypoint
{
    [AddComponentMenu("WrightAngle/Waypoint Target")]
    public class WaypointTarget : MonoBehaviour
    {
        [Tooltip("An optional name for this waypoint, primarily for identification in the editor or scripts.")]
        public string DisplayName = "";

        [Tooltip("If checked, this waypoint target will automatically register itself with the WaypointUIManager when the scene starts.")]
        public bool ActivateOnStart = true;

        // ←←← НОВАЯ НАСТРОЙКА ←←←
        [Header("Auto-Deactivation on Player Enter")]
        [Tooltip("Если включено — waypoint автоматически деактивируется, когда игрок входит в триггер этого объекта")]
        public bool DeactivateOnPlayerEnter = true;

        [Tooltip("Тег объекта, который считается «игроком» (по умолчанию Player)")]
        public string PlayerTag = "Player";

        // Опционально: можно повесить событие прямо в инспекторе
        public UnityEvent OnPlayerEntered;

        public bool IsRegistered { get; private set; } = false;

        public static event Action<WaypointTarget> OnTargetEnabled;
        public static event Action<WaypointTarget> OnTargetDisabled;

        private void Start()
        {
            // Если включён автостарт — регистрируемся сразу
            if (ActivateOnStart && gameObject.activeInHierarchy)
            {
                ActivateWaypoint();
            }
        }

        private void OnDisable()
        {
            ProcessDeactivation();
        }

        // ←←← ОСНОВНОЙ МЕТОД, КОТОРЫЙ НАС ИНТЕРЕСУЕТ ←←←
        private void OnTriggerEnter(Collider other)
        {
            if (!DeactivateOnPlayerEnter) return;

            // Проверяем по тегу (самый простой и быстрый способ)
            if (other.CompareTag(PlayerTag))
            {
                // Деактивируем waypoint (маркер исчезнет)
                DeactivateWaypoint();

                // Если нужно — вызываем дополнительные события
                OnPlayerEntered?.Invoke();

                // Опционально: полностью выключаем объект (если хочешь, чтобы и коллайдер пропал)
                // gameObject.SetActive(false);
            }
        }

        public void ActivateWaypoint()
        {
            if (!gameObject.activeInHierarchy || IsRegistered) return;

            OnTargetEnabled?.Invoke(this);
            IsRegistered = true;
        }

        public void DeactivateWaypoint()
        {
            ProcessDeactivation();
        }

        private void ProcessDeactivation()
        {
            if (!IsRegistered) return;

            OnTargetDisabled?.Invoke(this);
            IsRegistered = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsRegistered ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

#if UNITY_EDITOR
            string label = $"Waypoint: {gameObject.name}";
            if (!ActivateOnStart) label += " (Manual)";
            if (DeactivateOnPlayerEnter) label += " (Auto-off)";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, label);
#endif
        }
    }
}