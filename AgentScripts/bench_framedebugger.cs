// Frame Debugger를 켜고(이미 켜져 있으면 유지) 이벤트 수를 기록한 뒤 창을 캡처한다. (eval_file로 실행, LABEL은 실행 전에 바꾼다)
const string Label = "gpu_after";
var asm = typeof(UnityEditor.EditorWindow).Assembly;
var fdType = asm.GetType("UnityEditor.FrameDebuggerWindow");
var utilType = asm.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var win = UnityEditor.EditorWindow.GetWindow(fdType, true, "Frame Debugger", false);
win.position = new Rect(1600, 60, 1385, 525);
bool enabled = (int)utilType.GetProperty("count").GetValue(null) > 0;
if (!enabled) fdType.GetMethod("EnableFrameDebugger", flags).Invoke(win, null);
win.Repaint();
int count = (int)utilType.GetProperty("count").GetValue(null);

Rect pos = win.position;
Color[] pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(new Vector2(pos.x, pos.y), (int)pos.width, (int)pos.height);
var tex = new Texture2D((int)pos.width, (int)pos.height, TextureFormat.RGBA32, false);
tex.SetPixels(pixels);
tex.Apply();
System.IO.File.WriteAllBytes($"Docs/Optimization/images/{Label}_framedebugger.png", tex.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(tex);
return $"enabledBefore={enabled} events={count}";
