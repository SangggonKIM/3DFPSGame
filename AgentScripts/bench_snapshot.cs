// Memory Profiler 스냅샷(.snap)을 MemoryCaptures 폴더에 저장한다. (Play 모드에서 eval_file로 실행, LABEL은 실행 전에 바꾼다)
const string Label = "mem_after";
string path = System.IO.Path.GetFullPath($"MemoryCaptures/{Label}.snap");
Unity.Profiling.Memory.MemoryProfiler.TakeSnapshot(path, (p, ok) => Debug.Log($"[bench] snapshot {p} ok={ok}"),
    Unity.Profiling.Memory.CaptureFlags.ManagedObjects | Unity.Profiling.Memory.CaptureFlags.NativeObjects | Unity.Profiling.Memory.CaptureFlags.NativeAllocations);
return path;
