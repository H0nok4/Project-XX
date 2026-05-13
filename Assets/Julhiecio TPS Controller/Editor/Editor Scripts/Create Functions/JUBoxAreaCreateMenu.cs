using JU;
using UnityEditor;
using UnityEngine;

namespace JUTPSEditor
{
    public static class JUBoxAreaCreateMenu
    {
        [MenuItem("GameObject/JUTPS Create/Box Area", false, 0)]
        private static void CreateBox()
        {
            GameObject gameObject = new GameObject("JU Box Area");
            gameObject.AddComponent<JUBoxArea>();
            gameObject.transform.localScale = new Vector3(10f, 5f, 10f);
            gameObject.transform.position = SceneViewSpawnPosition() + (Vector3.up * 2.5f);
            gameObject.transform.rotation = SceneViewSpawnRotation();
        }

        private static Vector3 SceneViewSpawnPosition()
        {
            Camera sceneViewCamera = GetSceneViewCamera();
            if (sceneViewCamera == null)
                return Vector3.zero;

            Ray ray = sceneViewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit))
                return hit.point;

            return sceneViewCamera.transform.position + (sceneViewCamera.transform.forward * 10f);
        }

        private static Quaternion SceneViewSpawnRotation()
        {
            Camera sceneViewCamera = GetSceneViewCamera();
            if (sceneViewCamera == null)
                return Quaternion.identity;

            return Quaternion.Euler(Vector3.up * sceneViewCamera.transform.eulerAngles.y);
        }

        private static Camera GetSceneViewCamera()
        {
            return SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null;
        }
    }
}
