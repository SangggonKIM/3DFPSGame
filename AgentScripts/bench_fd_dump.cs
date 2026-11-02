// Frame Debugger 이벤트 목록을 텍스트로 저장한다. (Frame Debugger가 켜진 상태에서 eval_file로 실행, LABEL은 실행 전에 바꾼다)
const string Label = "gpu_before";
var util = typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditorInternal.FrameDebuggerInternal.FrameDebuggerUtility");
var events = (System.Array)util.GetMethod("GetFrameEvents").Invoke(null, null);
var getName = util.GetMethod("GetFrameEventInfoName");
var getObj = util.GetMethod("GetFrameEventObject");
var sb = new System.Text.StringBuilder();
var byType = new System.Collections.Generic.SortedDictionary<string, int>();
for (int i = 0; i < events.Length; i++)
{
    var ev = events.GetValue(i);
    string type = ev.GetType().GetField("m_Type", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.GetValue(ev)?.ToString() ?? "?";
    var obj = getObj.Invoke(null, new object[] { i }) as UnityEngine.Object;
    sb.AppendLine($"{i + 1}\t{type}\t{getName.Invoke(null, new object[] { i })}\t{(obj ? obj.name : "")}");
    byType[type] = byType.TryGetValue(type, out int c) ? c + 1 : 1;
}
var summary = new System.Text.StringBuilder($"# {Label}: total events {events.Length}\n");
foreach (var kv in byType) summary.AppendLine($"# {kv.Key}: {kv.Value}");
System.IO.File.WriteAllText($"Docs/Optimization/data/{Label}_framedebugger_events.tsv", summary.ToString() + sb.ToString());
return summary.ToString();
