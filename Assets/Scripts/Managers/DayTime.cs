using UnityEngine;



    public class Daytime : MonoBehaviour
    {
        [ExecuteInEditMode]
        [SerializeField] Gradient directLightGradient;
        [SerializeField] Gradient ambientLightGradient;

        [SerializeField, Range(1, 3660)] float timeInSeconds = 60;

        [SerializeField, Range(0f, 1f)] float timeProgress;

        [SerializeField] Light dirLight;

        Vector3 defaultAngles;

        void Start()
        {
            defaultAngles = dirLight.transform.localEulerAngles;
        }

        void Update()
        {
        if(Application.isPlaying)
            timeProgress += Time.deltaTime / timeInSeconds;

            if (timeProgress > 1f)
                timeProgress = 0f;

            dirLight.color = directLightGradient.Evaluate(timeProgress);
            RenderSettings.ambientLight = ambientLightGradient.Evaluate(timeProgress);

            dirLight.transform.localEulerAngles = new Vector3(360f * timeProgress - 90, defaultAngles.y, defaultAngles.z);
        }
    }
