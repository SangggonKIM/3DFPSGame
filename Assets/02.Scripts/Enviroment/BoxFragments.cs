using UnityEngine;

// GameObject를 지우는 것만으로는 런타임에 생성한 Mesh가 해제되지 않는다.
public class BoxFragments : MonoBehaviour
{
    private Mesh[] _meshes;

    private void Awake()
    {
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        _meshes = new Mesh[filters.Length];
        for (int i = 0; i < filters.Length; i++)
        {
            _meshes[i] = filters[i].sharedMesh;
        }
    }

    private void OnDestroy()
    {
        if (_meshes == null) return;

        foreach (Mesh mesh in _meshes)
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}
