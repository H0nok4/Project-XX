using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectXX.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXBootstrap : MonoBehaviour
    {
        [SerializeField] private bool autoLoadInitialScene = true;
        [SerializeField] private string initialSceneName = "ProjectXX_RaidTestMap";
        [SerializeField] private float loadDelaySeconds = 0.05f;

        private IEnumerator Start()
        {
            if (!autoLoadInitialScene || string.IsNullOrWhiteSpace(initialSceneName))
            {
                yield break;
            }

            if (SceneManager.GetActiveScene().name == initialSceneName)
            {
                yield break;
            }

            if (loadDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(loadDelaySeconds);
            }

            SceneManager.LoadScene(initialSceneName, LoadSceneMode.Single);
        }
    }
}
