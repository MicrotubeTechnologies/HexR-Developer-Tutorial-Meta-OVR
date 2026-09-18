using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace HexR
{
    /// <summary>
    /// Builds a minimal world-space connect panel out of plain UGUI: connect left, connect
    /// right, show hand colliders. Nothing else.
    ///
    /// The shipped "HexR Panel" prefab cannot serve this purpose. It carries 292 GameObjects
    /// and 203 MonoBehaviours, of which exactly three come from this package -- the rest are
    /// Meta Interaction SDK components. That makes it a hard dependency on the Meta SDK in a
    /// package that deliberately depends on neither backend: in an OpenXR-only project those
    /// 200 components resolve to missing scripts. Its material compounds it, pointing at a
    /// shader GUID this package does not ship, which is why the button renders magenta.
    ///
    /// This builds the panel from code instead of shipping a prefab, for the same reason
    /// Create Demo Scene generates its scene: nothing external gets serialised, so there is
    /// nothing to resolve and nothing to break. UGUI's Image with no sprite draws with
    /// UI/Default, one of the few shaders that renders correctly under both the Built-in
    /// pipeline and URP -- so this is pipeline-agnostic as well as backend-agnostic.
    /// </summary>
    public static class HexRSimplePanel
    {
        [MenuItem("HexR/Add Connect Panel", false, 4)]
        private static void AddConnectPanel()
        {
            GameObject panel = Build();
            Undo.RegisterCreatedObjectUndo(panel, "Add Connect Panel");

            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);
            EditorSceneManager.MarkSceneDirty(panel.scene);

            Debug.Log("[HexR] Added a connect panel. It finds HexRManager.Instance at run time and wires itself, "
                + "so there is nothing to assign in the Inspector -- just move it somewhere the user can reach.");
        }

        public static GameObject Build()
        {
            GameObject root = new GameObject("HexR Connect Panel",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(400f, 300f);
            rootRect.localScale = Vector3.one * 0.001f;      // 400 canvas units -> 0.4 m wide
            rootRect.position = new Vector3(0f, 1.2f, 0.5f);

            GameObject background = new GameObject("Background", typeof(Image));
            background.transform.SetParent(root.transform, false);

            RectTransform backRect = (RectTransform)background.transform;
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            // No sprite, so this draws with UI/Default -- present in every project, both
            // pipelines. Deliberately not a packaged material; see the class comment.
            background.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f, 0.94f);

            DefaultControls.Resources resources = BuiltinResources();

            CreateLabel(background.transform, resources, "Title", "HexR", new Vector2(0f, -30f), 26);

            Toggle left = CreateToggle(background.transform, resources, "Connect Left Glove", new Vector2(0f, -100f));
            Toggle right = CreateToggle(background.transform, resources, "Connect Right Glove", new Vector2(0f, -155f));
            Toggle colliders = CreateToggle(background.transform, resources, "Show Hand Colliders", new Vector2(0f, -210f));

            // The one script the panel needs. It waits for HexRManager.Instance and subscribes
            // itself, so none of these references cross the panel's own hierarchy -- which is
            // what let the old prefab ship with every UnityEvent target set to None.
            HexRPanelConnectButtons wiring = root.AddComponent<HexRPanelConnectButtons>();
            wiring.leftConnectToggle = left;
            wiring.rightConnectToggle = right;
            wiring.visualizerToggle = colliders;

            return root;
        }

        private static Toggle CreateToggle(Transform parent, DefaultControls.Resources resources,
            string label, Vector2 position)
        {
            GameObject go = DefaultControls.CreateToggle(resources);
            go.name = label;
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(330f, 44f);

            Transform labelTransform = go.transform.Find("Label");
            if (labelTransform != null)
            {
                Text text = labelTransform.GetComponent<Text>();
                if (text != null)
                {
                    text.text = label;
                    text.fontSize = 20;
                    text.color = Color.white;
                }

                RectTransform labelRect = (RectTransform)labelTransform;
                labelRect.offsetMin = new Vector2(38f, 0f);
                labelRect.offsetMax = Vector2.zero;
            }

            Toggle toggle = go.GetComponent<Toggle>();

            // Off to start with. HexRPanelConnectButtons only acts on the way on, and it pushes
            // the collider toggle's authored state into the visualizer at startup -- so a
            // ticked box here would mean opening the scene with colliders already drawn.
            toggle.isOn = false;
            return toggle;
        }

        private static void CreateLabel(Transform parent, DefaultControls.Resources resources,
            string name, string content, Vector2 position, int fontSize)
        {
            GameObject go = DefaultControls.CreateText(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(330f, 40f);

            Text text = go.GetComponent<Text>();
            if (text != null)
            {
                text.text = content;
                text.fontSize = fontSize;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }
        }

        // Unity's own built-in UI sprites, the same ones GameObject > UI > Toggle uses. They
        // live in the editor's extra resources, so they need no import and cannot go missing.
        private static DefaultControls.Resources BuiltinResources()
        {
            DefaultControls.Resources resources = new DefaultControls.Resources();
            resources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            resources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            resources.checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            return resources;
        }
    }
}
