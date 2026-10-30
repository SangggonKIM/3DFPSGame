using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// GameScene 정적 오브젝트에 LOD Group을 일괄 적용하는 에디터 헬퍼 (Assets 밖, 빌드 미포함)
public static class SetupLodGroups
{
    private const string ContainerHigh = "Cargo_container_v1_LD2close";
    private const string ContainerLow = "Cargo_container_v1_LD1close";
    private const string ContainerFbx = "Assets/RPG_FPS_game_assets_industrial/Containers/Cargo_container_v1/Source/Cargo_container_v1.FBX";

    // 화면 높이 대비 비율 (QualitySettings.lodBias가 곱해진다)
    private const float ContainerLod1Height = 0.25f;
    private const float ContainerCullHeight = 0.01f;
    private const float SmallPropCullHeight = 0.02f;

    // 멀리서는 그리지 않아도 되는 작은 소품 메시 이름 접두사
    private static readonly string[] SmallPropPrefixes =
    {
        "Palet_v1", "Wooden_box_v1", "Barrel_v1", "Barrel_v2", "Barrel_v3", "Conditioner_v1",
        "Pipes_set_v1_vent", "Road_block_v1", "Electric_box", "Generator_v1", "Dumpsters_v1",
    };

    public static string Run()
    {
        Mesh lowMesh = null;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(ContainerFbx))
        {
            if (asset is Mesh mesh && mesh.name == ContainerLow) lowMesh = mesh;
        }
        if (lowMesh == null) return "LD1close 메시를 찾지 못함";

        int containerCount = 0;
        int propCount = 0;
        var propNames = new Dictionary<string, int>();

        foreach (MeshRenderer renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            GameObject go = renderer.gameObject;
            if (GameObjectUtility.GetStaticEditorFlags(go) == 0) continue;
            if (go.GetComponent<LODGroup>() != null || go.GetComponentInParent<LODGroup>() != null) continue;

            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            string meshName = filter.sharedMesh.name;

            if (meshName == ContainerHigh)
            {
                MeshRenderer lowRenderer = CreateLowLodChild(renderer, lowMesh);
                LODGroup group = Undo.AddComponent<LODGroup>(go);
                group.SetLODs(new[]
                {
                    new LOD(ContainerLod1Height, new Renderer[] { renderer }),
                    new LOD(ContainerCullHeight, new Renderer[] { lowRenderer }),
                });
                group.RecalculateBounds();
                containerCount++;
                continue;
            }

            if (!IsSmallProp(meshName)) continue;

            LODGroup propGroup = Undo.AddComponent<LODGroup>(go);
            propGroup.SetLODs(new[] { new LOD(SmallPropCullHeight, new Renderer[] { renderer }) });
            propGroup.RecalculateBounds();
            propCount++;
            propNames.TryGetValue(meshName, out int count);
            propNames[meshName] = count + 1;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        var sb = new StringBuilder();
        sb.AppendLine($"컨테이너 LOD0/LOD1/Culled: {containerCount}");
        sb.AppendLine($"소품 LOD0/Culled: {propCount}");
        foreach (KeyValuePair<string, int> pair in propNames) sb.AppendLine($"  {pair.Key} x{pair.Value}");
        return sb.ToString();
    }

    private static bool IsSmallProp(string meshName)
    {
        foreach (string prefix in SmallPropPrefixes)
        {
            if (meshName.StartsWith(prefix)) return true;
        }
        return false;
    }

    private static MeshRenderer CreateLowLodChild(MeshRenderer source, Mesh lowMesh)
    {
        var child = new GameObject(source.name + "_LOD1");
        Undo.RegisterCreatedObjectUndo(child, "Create LOD1");
        child.transform.SetParent(source.transform, false);
        child.layer = source.gameObject.layer;
        GameObjectUtility.SetStaticEditorFlags(child, GameObjectUtility.GetStaticEditorFlags(source.gameObject));

        child.AddComponent<MeshFilter>().sharedMesh = lowMesh;
        MeshRenderer renderer = child.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = source.sharedMaterials;
        renderer.shadowCastingMode = source.shadowCastingMode;
        renderer.receiveShadows = source.receiveShadows;
        return renderer;
    }
}
