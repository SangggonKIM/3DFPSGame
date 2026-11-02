// 최적화 전후 측정 시작: Play 모드에서 Unity.Profiling.ProfilerRecorder를 켜고 Profiler/Stats 창을 준비한다. (eval_file로 실행)

var specs = new (Unity.Profiling.ProfilerCategory cat, string name)[]
{
    (Unity.Profiling.ProfilerCategory.Internal, "Main Thread"),
    (Unity.Profiling.ProfilerCategory.Internal, "PlayerLoop"),
    (Unity.Profiling.ProfilerCategory.Scripts, "BehaviourUpdate"),
    (Unity.Profiling.ProfilerCategory.Internal, "CPU Main Thread Frame Time"),
    (Unity.Profiling.ProfilerCategory.Internal, "CPU Render Thread Frame Time"),
    (Unity.Profiling.ProfilerCategory.Internal, "GPU Frame Time"),
    (Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame"),
    (Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count"),
    (Unity.Profiling.ProfilerCategory.Render, "Batches Count"),
    (Unity.Profiling.ProfilerCategory.Render, "SetPass Calls Count"),
    (Unity.Profiling.ProfilerCategory.Render, "Triangles Count"),
    (Unity.Profiling.ProfilerCategory.Render, "Vertices Count"),
    (Unity.Profiling.ProfilerCategory.Render, "Shadow Casters Count"),
    (Unity.Profiling.ProfilerCategory.Render, "Render Textures Count"),
    (Unity.Profiling.ProfilerCategory.Render, "Render Textures Bytes"),
    (Unity.Profiling.ProfilerCategory.Memory, "Total Used Memory"),
    (Unity.Profiling.ProfilerCategory.Memory, "GC Used Memory"),
    (Unity.Profiling.ProfilerCategory.Memory, "Gfx Used Memory"),
    (Unity.Profiling.ProfilerCategory.Memory, "Texture Memory"),
    (Unity.Profiling.ProfilerCategory.Memory, "Texture Count"),
    (Unity.Profiling.ProfilerCategory.Memory, "Mesh Memory"),
};

var old = System.AppDomain.CurrentDomain.GetData("bench") as System.Collections.Generic.List<(string, Unity.Profiling.ProfilerRecorder)>;
if (old != null) foreach (var o in old) o.Item2.Dispose();

var list = new System.Collections.Generic.List<(string, Unity.Profiling.ProfilerRecorder)>();
foreach (var s in specs) list.Add((s.name, Unity.Profiling.ProfilerRecorder.StartNew(s.cat, s.name, 300)));
System.AppDomain.CurrentDomain.SetData("bench", list);
FrameTimingManager.IsFeatureEnabled();

// Game 창 Stats 오버레이 켜기
var gameViewType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");
var gameView = (UnityEditor.EditorWindow)Resources.FindObjectsOfTypeAll(gameViewType)[0];
var statsField = gameViewType.GetField("m_Stats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
statsField?.SetValue(gameView, true);
gameView.Repaint();

// Profiler 창을 Scene/Hierarchy 영역 위에 띄워 Game 창을 가리지 않게 한다.
var profilerType = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.ProfilerWindow");
var profiler = UnityEditor.EditorWindow.GetWindow(profilerType, true, "Profiler", false);
profiler.position = new Rect(1600, 60, 1385, 525);
UnityEditorInternal.ProfilerDriver.enabled = true;

int valid = 0;
foreach (var r in list) if (r.Item2.Valid) valid++;
return $"recorders {valid}/{list.Count} valid, statsField={(statsField != null)}";
