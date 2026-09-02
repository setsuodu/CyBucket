using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

/// <summary>
/// 场景 UIDocument → 注入 UIService
/// </summary>
public class UIDocumentBinder : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private void Start()
    {
        if (document == null) document = GetComponent<UIDocument>();
        var scope = FindObjectOfType<AppLifetimeScope>();
        if (scope == null)
        {
            Debug.LogError("AppLifetimeScope missing");
            return;
        }
        var ui = scope.Container.Resolve<UIService>();
        ui.BindDocument(document);
    }
}
