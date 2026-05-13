using System.IO;
using JUTPS.DestructibleSystem;
using JUTPS.FX;
using UnityEditor;
using UnityEngine;

namespace JUTPS.DestructibleSystem
{
    [CustomEditor(typeof(FractureTool))]
    public sealed class FractureToolEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FractureTool fractureTool = (FractureTool)target;

            if (GUILayout.Button("Generate Fractured Object"))
            {
                fractureTool.DestroyMesh();
            }

            if (GUILayout.Button("Save Generated Meshes as asset"))
            {
                FractureToolAssetUtility.SaveFracturedAssets(fractureTool);
            }
        }
    }

    internal static class FractureToolAssetUtility
    {
        private const string GeneratedFracturesFolder = "Julhiecio TPS Controller/Generated Fractures";

        public static void SaveFracturedAssets(FractureTool fractureTool)
        {
            if (!fractureTool)
                return;

            GameObject fracturedObject = fractureTool.GeneratedFracturedObject;
            if (fracturedObject == null)
            {
                Debug.LogError("There is no linked fractured game object. Click on ''Generate Fractured Object'' or link the fractured object and then click on ''Save Generated Meshes as asset''");
                return;
            }

            string rootPath = Path.Combine(Application.dataPath, GeneratedFracturesFolder);
            Directory.CreateDirectory(rootPath);
            Debug.Log("Path to save fractures: " + rootPath);

            int childCount = fracturedObject.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                GameObject childFracture = fracturedObject.transform.GetChild(i).gameObject;
                MeshFilter meshFilter = childFracture.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                SaveFractureMesh(
                    meshFilter.sharedMesh,
                    childFracture.name + "_fracture_" + i,
                    meshOptimization: true,
                    pathName: childFracture.name,
                    rootPath: rootPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(rootPath);
        }

        private static void SaveFractureMesh(Mesh fractureMesh, string name, bool meshOptimization, string pathName, string rootPath)
        {
            string outputDirectory = Path.Combine(rootPath, pathName + "_meshes_fractures");
            Directory.CreateDirectory(outputDirectory);

            string absolutePath = Path.Combine(outputDirectory, name + ".asset");
            string projectRelativePath = FileUtil.GetProjectRelativePath(absolutePath);
            if (string.IsNullOrEmpty(projectRelativePath))
                return;

            projectRelativePath = AssetDatabase.GenerateUniqueAssetPath(projectRelativePath);

            Mesh meshToSave = Object.Instantiate(fractureMesh);
            if (meshOptimization)
                MeshUtility.Optimize(meshToSave);

            AssetDatabase.CreateAsset(meshToSave, projectRelativePath);
            Debug.Log("Meshes saved at: " + projectRelativePath);
        }
    }

    internal static class JUFootstepDefaultsMenu
    {
        private const string FootstepAudioRootFolder = "Assets/Julhiecio TPS Controller/Audio/Footstep";

        [MenuItem("CONTEXT/JUFootstep/Load Default Footstep Audios", false, 100)]
        private static void LoadDefaultFootstepAudios(MenuCommand command)
        {
            JUFootstep footstep = command.context as JUFootstep;
            if (footstep == null)
                return;

            ApplyDefaultFootstepAudios(footstep);
        }

        internal static void ApplyDefaultFootstepAudios(JUFootstep footstep)
        {
            if (footstep == null)
                return;

            if (!AssetDatabase.IsValidFolder(FootstepAudioRootFolder))
            {
                Debug.LogError("Unable to load default footstep audios as the indicated folder does not exist.");
                return;
            }

            Undo.RecordObject(footstep, "Load Default Footstep Audios");
            footstep.FootstepAudioClips = CreateDefaultFootstepAudioClips();
            PrefabUtility.RecordPrefabInstancePropertyModifications(footstep);
            EditorUtility.SetDirty(footstep);
        }

        private static SurfaceAudiosWithFX[] CreateDefaultFootstepAudioClips()
        {
            return new[]
            {
                CreateSurface(
                    "Untagged",
                    $"{FootstepAudioRootFolder}/Concrete/Footstep on Concrete 01.ogg",
                    $"{FootstepAudioRootFolder}/Concrete/Footstep on Concrete 02.ogg",
                    $"{FootstepAudioRootFolder}/Concrete/Footstep on Concrete 03.ogg",
                    $"{FootstepAudioRootFolder}/Concrete/Footstep on Concrete 04.ogg"),
                CreateSurface(
                    "Stone",
                    $"{FootstepAudioRootFolder}/Stones/Footsteps-on-stone01.ogg",
                    $"{FootstepAudioRootFolder}/Stones/Footsteps-on-stone02.ogg",
                    $"{FootstepAudioRootFolder}/Stones/Footsteps-on-stone03.ogg",
                    $"{FootstepAudioRootFolder}/Stones/Footsteps-on-stone04.ogg"),
                CreateSurface(
                    "Grass",
                    $"{FootstepAudioRootFolder}/Grass/Footsteps-on-grass01.ogg",
                    $"{FootstepAudioRootFolder}/Grass/Footsteps-on-grass02.ogg",
                    $"{FootstepAudioRootFolder}/Grass/Footsteps-on-grass03.ogg",
                    $"{FootstepAudioRootFolder}/Grass/Footsteps-on-grass04.ogg"),
                CreateSurface(
                    "Tiles",
                    $"{FootstepAudioRootFolder}/Tiles/Footstep-on-tiles01.ogg",
                    $"{FootstepAudioRootFolder}/Tiles/Footstep-on-tiles02.ogg",
                    $"{FootstepAudioRootFolder}/Tiles/Footstep-on-tiles03.ogg",
                    $"{FootstepAudioRootFolder}/Tiles/Footstep-on-tiles04.ogg"),
            };
        }

        private static SurfaceAudiosWithFX CreateSurface(string surfaceTag, params string[] clipPaths)
        {
            SurfaceAudiosWithFX surface = new SurfaceAudiosWithFX
            {
                SurfaceTag = surfaceTag
            };

            foreach (string clipPath in clipPaths)
                surface.AudioClips.Add(LoadAsset<AudioClip>(clipPath));

            return surface;
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"Unable to load asset {typeof(T).Name}: {path}");

            return asset;
        }
    }
}
