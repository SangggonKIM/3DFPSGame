// Memory Profiler 창에 스냅샷을 연다. COMPARE가 비어 있지 않으면 비교 모드로 두 스냅샷을 연다. (eval_file로 실행)
const string Base = "mem_after";
const string Compare = "";
var asm = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Unity.MemoryProfiler.Editor");
var wt = asm.GetType("Unity.MemoryProfiler.Editor.MemoryProfilerWindow");
var win = (UnityEditor.EditorWindow)Resources.FindObjectsOfTypeAll(wt)[0];
win.position = new Rect(1600, 60, 1385, 900);
var svc = wt.GetField("m_SnapshotDataService", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(win);
var st = svc.GetType();
st.GetMethod("UnloadAll", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(svc, null);
st.GetMethod("Load", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(svc, new object[] { System.IO.Path.GetFullPath($"MemoryCaptures/{Base}.snap") });
if (Compare != "")
{
    st.GetProperty("CompareMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).SetValue(svc, true);
    st.GetMethod("Load", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Invoke(svc, new object[] { System.IO.Path.GetFullPath($"MemoryCaptures/{Compare}.snap") });
}
win.Repaint();
return "loaded";
