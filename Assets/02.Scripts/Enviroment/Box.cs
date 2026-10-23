using UnityEngine;

// 공격을 받으면 OpenFracture로 메시를 잘라 물리 파편을 만든다.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Box : MonoBehaviour, IDamageable
{
    [Header("파괴 설정")]
    [SerializeField, Range(2, 64)] private int _fragmentCount = 20;
    [SerializeField, Min(0f)] private float _breakForce = 4f;
    [SerializeField, Min(0f)] private float _spinForce = 2f;
    [SerializeField, Min(0.1f)] private float _fragmentLifetime = 5f;
    [SerializeField] private Material _insideMaterial;

    private bool _isBroken;

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_isBroken || damageInfo.Value <= 0f) return;

        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1)
        {
            Debug.LogWarning("상자 파괴에는 읽기 가능한 단일 서브메시가 필요합니다. 모델의 Read/Write 설정을 확인해주세요.", this);
            return;
        }

        _isBroken = true;
        Break(damageInfo);
    }

    private void Break(DamageInfo damageInfo)
    {
        var sourceCollider = GetComponent<Collider>();
        if (sourceCollider == null) sourceCollider = gameObject.AddComponent<BoxCollider>();

        // OpenFracture가 파편 질량을 계산할 때 원본 Rigidbody를 사용한다.
        var sourceBody = GetComponent<Rigidbody>();
        if (sourceBody == null)
        {
            sourceBody = gameObject.AddComponent<Rigidbody>();
            sourceBody.isKinematic = true;
        }

        var fragments = new GameObject($"{name}_Fragments");
        fragments.transform.SetParent(transform.parent, false);
        fragments.transform.SetPositionAndRotation(transform.position, transform.rotation);

        var template = new GameObject("BoxFragment");
        template.SetActive(false);
        template.AddComponent<MeshFilter>();
        var fragmentRenderer = template.AddComponent<MeshRenderer>();
        Material outsideMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        fragmentRenderer.sharedMaterials = new[]
        {
            outsideMaterial,
            _insideMaterial != null ? _insideMaterial : outsideMaterial
        };

        var fragmentCollider = template.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        fragmentCollider.sharedMaterial = sourceCollider.sharedMaterial;
        var templateBody = template.AddComponent<Rigidbody>();
        templateBody.linearVelocity = sourceBody.linearVelocity;
        templateBody.angularVelocity = sourceBody.angularVelocity;

        var options = new FractureOptions
        {
            fragmentCount = Mathf.Clamp(_fragmentCount, 2, 64),
            insideMaterial = fragmentRenderer.sharedMaterials[1]
        };

        try
        {
            Fragmenter.Fracture(gameObject, options, template, fragments.transform);
        }
        catch
        {
            // 생성 도중 실패해도 이미 만들어진 파편 메시를 정리한다.
            fragments.AddComponent<BoxFragments>();
            Destroy(fragments);
            _isBroken = false;
            throw;
        }
        finally
        {
            Destroy(template);
        }

        Bounds bounds = GetComponent<MeshRenderer>().bounds;
        float radius = Mathf.Max(bounds.extents.magnitude * 2f, 0.1f);
        foreach (Transform fragment in fragments.transform)
        {
            fragment.gameObject.layer = gameObject.layer;
            fragment.gameObject.SetActive(true);
            var body = fragment.GetComponent<Rigidbody>();
            body.AddExplosionForce(_breakForce, damageInfo.HitPoint, radius, 0.3f,
                ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * _spinForce, ForceMode.Impulse);
        }

        fragments.AddComponent<BoxFragments>();
        Destroy(fragments, Mathf.Max(0.1f, _fragmentLifetime));
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
