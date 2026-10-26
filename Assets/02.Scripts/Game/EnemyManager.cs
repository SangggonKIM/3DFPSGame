using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance => _instance;
    private List<Zombie> _zombieList = new List<Zombie>();
    public List<Zombie> ZombieList => _zombieList;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            return;
        }

        Destroy(gameObject);
    }

    public void AddZombie(Zombie zombie)
    {
        _zombieList.Add(zombie);
    }

    public void RemoveZombie(Zombie zombie)
    {
        _zombieList.Remove(zombie);
    }

    public int GetEnemyCount()
    {
        return _zombieList.Count;
    }
}