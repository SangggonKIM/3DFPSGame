using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// 라이트 프로브 배치 + 외삽 폭주 검증 에디터 헬퍼 (Assets 밖, 빌드 미포함)
public static class SetupLightProbes
{
    private const string GroupName = "Light Probes";
    private const float MinX = -52f;
    private const float MaxX = 102f;
    private const float MinZ = -86f;
    private const float MaxZ = 62f;
    private const float RayStartY = 60f;
    private const float RayLength = 120f;

    // 정상 SH 계수는 0.5 미만. 이보다 크면 외삽이 폭주한 것으로 본다.
    private const float BrokenThreshold = 2f;

    private static readonly float[] LayerHeights = { 0.3f, 2f, 5f };
    private static readonly float[] SampleHeights = { 0.1f, 0.5f, 1f, 1.8f };

    public static string Place(float spacing)
    {
        LightProbeGroup group = GetOrCreateGroup();
        var positions = new List<Vector3>();
        Physics.SyncTransforms();

        for (float x = MinX; x <= MaxX; x += spacing)
        {
            for (float z = MinZ; z <= MaxZ; z += spacing)
            {
                if (!TryGetGround(x, z, out Vector3 ground)) continue;
                foreach (float height in LayerHeights) positions.Add(ground + Vector3.up * height);
            }
        }

        Undo.RecordObject(group, "Place Light Probes");
        group.probePositions = positions.ToArray();
        EditorUtility.SetDirty(group);
        EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
        return $"probes={positions.Count}";
    }

    // 베이크된 프로브로 걸을 수 있는 지점을 훑어, 보간값이 폭주하는 지점 수를 센다.
    public static string Validate(float step)
    {
        List<Vector3> broken = FindBroken(step, out int sampled, out float worst);
        var sb = new StringBuilder();
        sb.AppendLine($"sampled={sampled} broken={broken.Count} worstCoeff={worst}");
        for (int i = 0; i < Mathf.Min(10, broken.Count); i++) sb.AppendLine($"  {broken[i]}");
        return sb.ToString();
    }

    // 폭주 지점 바로 위에 프로브 기둥을 추가한다. 다시 베이크해야 반영된다.
    public static string Refine(float step)
    {
        List<Vector3> broken = FindBroken(step, out _, out _);
        LightProbeGroup group = GetOrCreateGroup();
        var positions = new List<Vector3>(group.probePositions);
        var added = new HashSet<Vector2Int>();

        foreach (Vector3 point in broken)
        {
            var key = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.z));
            if (!added.Add(key)) continue;
            if (!TryGetGround(point.x, point.z, out Vector3 ground)) continue;
            foreach (float height in LayerHeights) positions.Add(ground + Vector3.up * height);
        }

        Undo.RecordObject(group, "Refine Light Probes");
        group.probePositions = positions.ToArray();
        EditorUtility.SetDirty(group);
        EditorSceneManager.MarkSceneDirty(group.gameObject.scene);
        return $"added columns={added.Count} probes={positions.Count}";
    }

    private static List<Vector3> FindBroken(float step, out int sampled, out float worst)
    {
        var broken = new List<Vector3>();
        sampled = 0;
        worst = 0f;
        Physics.SyncTransforms();

        for (float x = MinX; x <= MaxX; x += step)
        {
            for (float z = MinZ; z <= MaxZ; z += step)
            {
                if (!TryGetGround(x, z, out Vector3 ground)) continue;
                foreach (float height in SampleHeights)
                {
                    Vector3 point = ground + Vector3.up * height;
                    LightProbes.GetInterpolatedProbe(point, null, out SphericalHarmonicsL2 sh);
                    float max = MaxAbs(sh);
                    sampled++;
                    worst = Mathf.Max(worst, max);
                    if (max > BrokenThreshold || float.IsNaN(max)) broken.Add(point);
                }
            }
        }
        return broken;
    }

    private static float MaxAbs(SphericalHarmonicsL2 sh)
    {
        float max = 0f;
        for (int channel = 0; channel < 3; channel++)
        {
            for (int coefficient = 0; coefficient < 9; coefficient++)
            {
                float value = sh[channel, coefficient];
                if (float.IsNaN(value) || float.IsInfinity(value)) return float.NaN;
                max = Mathf.Max(max, Mathf.Abs(value));
            }
        }
        return max;
    }

    private static bool TryGetGround(float x, float z, out Vector3 ground)
    {
        bool hit = Physics.Raycast(new Vector3(x, RayStartY, z), Vector3.down, out RaycastHit info, RayLength, ~0, QueryTriggerInteraction.Ignore);
        ground = info.point;
        return hit;
    }

    private static LightProbeGroup GetOrCreateGroup()
    {
        GameObject go = GameObject.Find(GroupName);
        if (go == null)
        {
            go = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(go, "Create Light Probes");
        }

        LightProbeGroup group = go.GetComponent<LightProbeGroup>();
        if (group == null) group = Undo.AddComponent<LightProbeGroup>(go);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        return group;
    }
}
