// CPU 측정 조건 재현: 씬의 좀비를 복제해 40마리로 맞춘다. (eval_file로 실행)
var zombies = UnityEngine.Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None);
var src = zombies[0];
int need = 40 - zombies.Length;
for (int i = 0; i < need; i++)
{
    Vector3 offset = new Vector3((i % 8) * 2f - 7f, 0f, (i / 8) * 2f + 2f);
    UnityEngine.Object.Instantiate(src.gameObject, src.transform.position + offset, src.transform.rotation);
}
return "zombies=" + UnityEngine.Object.FindObjectsByType<Zombie>(FindObjectsSortMode.None).Length;
