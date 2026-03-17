using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class EnemyClusterData
{
    [FormerlySerializedAs("clusterBeginTime")] [SerializeField, ReadOnly] private string clusterName;

    [Header("WHO"), Tooltip("If multiple enemies are assigned, then they all spawn together")]
    public GameObject[] enemyPrefab;

    [FormerlySerializedAs("timeToSpawn")]
    [Header("WHEN")]
    [Min(0)] public float spawnStartTime;
    
    [Header("HOW MANY REPETITIONS")]
    [Min(1)] public int repeatCount;
    
    [Header("AT WHAT INTERVAL")]
    [Min(0)] public float timeBetweenSpawns;

    [Header("GROUP ATTACK?")]
    public bool spawnAsGroup = true;

    [Header("UNTIL")]
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
