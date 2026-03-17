using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EnemyClusterData
{
    [FormerlySerializedAs("clusterBeginTime")] [SerializeField, ReadOnly] private string clusterName;

    public GameObject[] enemyPrefab;
    [FormerlySerializedAs("timeToSpawn")]
    [Min(0)] public float spawnStartTime;
    [Min(1)] public int repeatCount;
    [Min(0)] public float timeBetweenSpawns;
    public bool spawnAsGroup = true;
    [SerializeField, ReadOnly] private float stopsSpawningAtTime;

    public void UpdateClusterName()
    {
        clusterName = enemyPrefab[0].name + " | " + spawnStartTime.ToString(CultureInfo.CurrentCulture) + " -> " + (repeatCount * timeBetweenSpawns + spawnStartTime - timeBetweenSpawns);

        stopsSpawningAtTime = repeatCount * timeBetweenSpawns + spawnStartTime - timeBetweenSpawns;
    }
}

[Serializable]
public class Wave
{
    [ReadOnly] public string waveName;
    
    public int currencyPrize;
    
    public List<EnemyClusterData> enemyClusters = new();
}
