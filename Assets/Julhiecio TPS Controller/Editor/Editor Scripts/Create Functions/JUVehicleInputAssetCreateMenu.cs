using JUTPS.VehicleSystem.Inputs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace JUTPSEditor
{
    public static class JUVehicleInputAssetCreateMenu
    {
        [MenuItem("Assets/Create/JU TPS/Vehicles/Classic Vehicle Input", false, 1)]
        private static void CreateClassicInputAsset()
        {
            CreateAsset<JUVehicleInputAsset>("Classic Vehicle Input", instance =>
            {
                instance.ThrottleAction.AddCompositeBinding("Axis")
                    .With("Positive", "<Gamepad>/leftStick/up")
                    .With("Negative", "<Gamepad>/leftStick/down")
                    .With("Positive", "<Keyboard>/w")
                    .With("Negative", "<Keyboard>/s");

                instance.SteerAction.AddCompositeBinding("Axis")
                    .With("Positive", "<Gamepad>/leftStick/right")
                    .With("Negative", "<Gamepad>/leftStick/left")
                    .With("Positive", "<Keyboard>/d")
                    .With("Negative", "<Keyboard>/a");

                instance.BrakeAction.AddBinding("<Gamepad>/buttonEast");
                instance.BrakeAction.AddBinding("<Keyboard>/space");

                instance.NitroAction.AddBinding("<Keyboard>/shift");
                instance.NitroAction.AddBinding("<Gamepad>/leftStickPress");
            });
        }

        [MenuItem("Assets/Create/JU TPS/Vehicles/Advanced Vehicle Input", false, 1)]
        private static void CreateAdvancedInputAsset()
        {
            CreateAsset<JUVehicleInputAsset>("Advanced Vehicle Input", instance =>
            {
                instance.ThrottleAction.AddCompositeBinding("Axis")
                    .With("Positive", "<Gamepad>/rightTrigger")
                    .With("Negative", "<Gamepad>/leftTrigger")
                    .With("Positive", "<Keyboard>/w")
                    .With("Negative", "<Keyboard>/s");

                instance.SteerAction.AddCompositeBinding("Axis")
                    .With("Positive", "<Gamepad>/leftStick/right")
                    .With("Negative", "<Gamepad>/leftStick/left")
                    .With("Positive", "<Keyboard>/d")
                    .With("Negative", "<Keyboard>/a");

                instance.BrakeAction.AddBinding("<Gamepad>/buttonEast");
                instance.BrakeAction.AddBinding("<Keyboard>/space");

                instance.NitroAction.AddBinding("<Keyboard>/shift");
                instance.NitroAction.AddBinding("<Gamepad>/leftStickPress");
            });
        }

        private static void CreateAsset<T>(string assetName, UnityAction<T> onCreated) where T : ScriptableObject
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                T instance = ScriptableObject.CreateInstance<T>();
                string path = AssetDatabase.GetAssetPath(Selection.activeInstanceID);

                if (string.IsNullOrEmpty(path))
                {
                    path = "Assets";
                }

                if (path.Contains("."))
                {
                    path = path.Remove(path.LastIndexOf('/'));
                }

                string pathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{assetName}.asset");
                AssetDatabase.CreateAsset(instance, pathAndName);

                onCreated?.Invoke(instance);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = instance;
                EditorUtility.FocusProjectWindow();
                EditorUtility.SetDirty(instance);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("Can't create a new asset on this folder.");
                Debug.LogError(exception.Message);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
