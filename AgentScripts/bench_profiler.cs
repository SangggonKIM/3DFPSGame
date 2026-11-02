// 일시정지 상태에서 Profiler 창(Hierarchy 포함)을 캡처한다. (eval_file로 실행, LABEL은 실행 전에 바꾼다)
const string Label = "cpu_after";
var type = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ProfilerWindow");
var window = (UnityEditor.EditorWindow)Resources.FindObjectsOfTypeAll(type)[0];
Rect pos = window.position;
int w = (int)pos.width, h = (int)pos.height;
Color[] pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(new Vector2(pos.x, pos.y), w, h);
var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
tex.SetPixels(pixels);
tex.Apply();
System.IO.File.WriteAllBytes($"Docs/Optimization/images/{Label}_profiler.png", tex.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(tex);
return "ok";
