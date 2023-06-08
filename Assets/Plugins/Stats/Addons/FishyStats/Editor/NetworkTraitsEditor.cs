using Stats.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Stats.FishNet.Editor
{
    [CustomEditor(typeof(NetworkTraits))]
    [CanEditMultipleObjects]
    public class NetworkTraitsEditor : UnityEditor.Editor
    {
        private TraitsVisualElement _traitsVisualElement;
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            _traitsVisualElement = new TraitsVisualElement(target as NetworkTraits, serializedObject);
            root.Add(_traitsVisualElement);
            return root;
        }

        private void OnDestroy() => _traitsVisualElement?.ReleaseAllCallback();
    }
}