using Stats.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Stats.FishNet.Editor
{
    [CustomEditor(typeof(NetworkTraits))]
    [CanEditMultipleObjects]
    public class NetworkTraitsEditor : UnityEditor.Editor
    {
        private TraitsElement _traitsVisualElement;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            SerializedProperty property = serializedObject.FindProperty("_traitsClassRegistry");
            root.Add(new PropertyField(property));

            _traitsVisualElement = new TraitsElement(target as NetworkTraits, serializedObject);
            root.Add(_traitsVisualElement);
            return root;
        }

        private void OnDestroy() => _traitsVisualElement?.ReleaseAllCallback();
    }
}
