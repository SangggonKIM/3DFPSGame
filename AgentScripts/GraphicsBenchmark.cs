using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// 그래픽 최적화 전후 비교용 측정 헬퍼 (Assets 밖에 있어 게임 빌드에 포함되지 않음)
public static class GraphicsBenchmark
{
    private struct View
    {
        public string Name;
        public Vector3 Position;
        public Vector3 Euler;
    }

    private static readonly View[] Views =
    {
        new View { Name = "view1_street", Position = new Vector3(0f, 1.7f, -3f), Euler = new Vector3(5f, 90f, 0f) },
        new View { Name = "view2_overview", Position = new Vector3(-50f, 45f, -85f), Euler = new Vector3(25f, 45f, 0f) },
        new View { Name = "view3_behind_hangar", Position = new Vector3(20f, 1.7f, 16f), Euler = new Vector3(0f, 90f, 0f) },
    };

    // 카메라 추종/회전 스크립트를 끄고 고정 시점으로 옮긴다.
    public static string SetView(int index)
    {
        Camera cam = Camera.main;

        // CameraViewSwitcher는 꺼질 때 CameraFollow/CameraRotate를 원래 상태로 되돌리므로 가장 먼저 끈다.
        DisableBehaviour(cam, "CameraViewSwitcher");
        DisableBehaviour(cam, "CameraFollow");
        DisableBehaviour(cam, "CameraRotate");

        View view = Views[index];
        cam.transform.SetPositionAndRotation(view.Position, Quaternion.Euler(view.Euler));
        return view.Name;
    }

    private static void DisableBehaviour(Camera cam, string typeName)
    {
        foreach (MonoBehaviour behaviour in cam.GetComponents<MonoBehaviour>())
        {
            if (behaviour.GetType().Name == typeName) behaviour.enabled = false;
        }
    }

    // Stats 오버레이가 켜진 Game 창을 화면 픽셀 그대로 저장하고, 수치를 한 줄 JSON으로 돌려준다.
    public static string CaptureStats(string label, string viewName)
    {
        System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow gameView = (EditorWindow)Resources.FindObjectsOfTypeAll(gameViewType)[0];

        Rect pos = gameView.position;
        float ppp = EditorGUIUtility.pixelsPerPoint;
        int width = (int)(pos.width * ppp);
        int height = (int)(pos.height * ppp);
        Color[] pixels = InternalEditorUtility.ReadScreenPixel(new Vector2(pos.x * ppp, pos.y * ppp), width, height);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();

        string dir = "Docs/GraphicsOptimization";
        Directory.CreateDirectory(dir);
        File.WriteAllBytes($"{dir}/{label}_{viewName}_stats.png", texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        string json = "{"
            + $"\"label\":\"{label}\",\"view\":\"{viewName}\","
            + $"\"batches\":{UnityStats.batches},\"setPassCalls\":{UnityStats.setPassCalls},"
            + $"\"drawCalls\":{UnityStats.drawCalls},\"triangles\":{UnityStats.triangles},"
            + $"\"vertices\":{UnityStats.vertices},\"shadowCasters\":{UnityStats.shadowCasters},"
            + $"\"frameTimeMs\":{UnityStats.frameTime * 1000f:F2},\"renderTimeMs\":{UnityStats.renderTime * 1000f:F2}"
            + "}";
        File.AppendAllText($"{dir}/stats.jsonl", json + "\n");
        return json;
    }
}
