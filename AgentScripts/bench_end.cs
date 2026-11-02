// 최적화 전후 측정 종료: 최근 300프레임 통계를 JSON으로 저장하고 Game(Stats)/Profiler 창을 캡처한다. (eval_file로 실행)
// LABEL은 실행 전에 바꾼다.

const string Label = "cpu_after";
const string Dir = "Docs/Optimization";

var list = System.AppDomain.CurrentDomain.GetData("bench") as System.Collections.Generic.List<(string, Unity.Profiling.ProfilerRecorder)>;
if (list == null) return "bench_start.cs 먼저 실행";

var inv = System.Globalization.CultureInfo.InvariantCulture;
var sb = new System.Text.StringBuilder();
sb.Append("{\n  \"label\": \"" + Label + "\",\n  \"metrics\": {\n");
bool first = true;
foreach (var (name, rec) in list)
{
    if (!rec.Valid || rec.Count == 0) continue;
    var values = new System.Collections.Generic.List<double>();
    for (int i = 0; i < rec.Count; i++) values.Add(rec.GetSample(i).Value);
    bool isTime = rec.UnitType == Unity.Profiling.ProfilerMarkerDataUnit.TimeNanoseconds;
    double scale = isTime ? 1e-6 : 1.0;
    values.Sort();
    double avg = 0; foreach (var v in values) avg += v; avg /= values.Count;
    double p50 = values[values.Count / 2], p95 = values[(int)(values.Count * 0.95)], max = values[values.Count - 1];
    string key = isTime ? name + " (ms)" : name;
    if (!first) sb.Append(",\n");
    first = false;
    sb.Append(string.Format(inv, "    \"{0}\": {{\"avg\": {1:0.###}, \"p50\": {2:0.###}, \"p95\": {3:0.###}, \"max\": {4:0.###}, \"frames\": {5}}}",
        key, avg * scale, p50 * scale, p95 * scale, max * scale, values.Count));
}
sb.Append("\n  }\n}\n");
System.IO.Directory.CreateDirectory(Dir + "/data");
System.IO.Directory.CreateDirectory(Dir + "/images");
System.IO.File.WriteAllText($"{Dir}/data/{Label}.json", sb.ToString());

// 창을 화면 픽셀 그대로 캡처
void Capture(string typeName, string file)
{
    var type = typeof(UnityEditor.EditorWindow).Assembly.GetType(typeName);
    var found = Resources.FindObjectsOfTypeAll(type);
    if (found.Length == 0) return;
    var window = (UnityEditor.EditorWindow)found[0];
    window.Repaint();
    Rect pos = window.position;
    float ppp = UnityEditor.EditorGUIUtility.pixelsPerPoint;
    int w = (int)(pos.width * ppp), h = (int)(pos.height * ppp);
    Color[] pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(new Vector2(pos.x * ppp, pos.y * ppp), w, h);
    var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
    tex.SetPixels(pixels);
    tex.Apply();
    System.IO.File.WriteAllBytes($"{Dir}/images/{file}", tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
}
Capture("UnityEditor.GameView", Label + "_stats.png");
// 프로파일러 Hierarchy가 보이도록 일시정지한다. 캡처는 bench_profiler.cs에서.
UnityEditor.EditorApplication.isPaused = true;

return sb.ToString();
