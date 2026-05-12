using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject dasherPrefab;
    public GameObject bossPrefab;
    public GameObject exploderPrefab;
    public GameObject spitterPrefab;
    public GameObject healthPickupPrefab;
    public GameObject bulletAmmoPickupPrefab;
    public GameObject shellAmmoPickupPrefab;

    [Header("Spawn Settings")]
    public Transform player;
    public Transform healthPickupPoint;
    public float minSpawnDistance = 14f;
    public float maxSpawnDistance = 18f;
    public float baseSpawnRate = 2.0f;
    [SerializeField] private float timeBetweenWaves = 2.5f;

    [Header("Ammo Drops")]
    [SerializeField] private float ammoDropChance = 0.2f;
    [SerializeField] private float normalWaveAmmoCoverage = 0.4f;
    [SerializeField] private float bossWaveAmmoCoverage = 0.5f;

    [Header("Wave Info")]
    public int currentWave = 0;

    private bool isSpawning;
    private bool waitingForNextWave;
    private float nextWaveStartTime;
    private GameObject currentHealthPickup;
    private WeaponController playerWeaponController;

    private int enemiesInWave;
    private int enemiesKilledThisWave;
    private int guaranteedAmmoPacks;
    private int ammoPacksDropped;

    private const int BulletPackAmount = 30;
    private const int ShellPackAmount = 6;
    private const float HitAccuracy = 0.9f;
    private const float AverageBulletDamage = 20f;
    private const float AverageShellShotDamage = 72f;

    private void Awake()
    {
        Instance = this;
        ResolvePlayer();

        currentWave = 0;
        isSpawning = false;
        waitingForNextWave = true;
        nextWaveStartTime = Time.time + 1f;

        GameManager.ResetStats();
        GameManager.ReportWaveState(false);
    }

    private void Update()
    {
        if (!GameManager.CanGameplayRun()) return;
        if (player == null || isSpawning) return;

        bool arenaIsClear = GameManager.GetAliveEnemies() <= 0;
        if (arenaIsClear)
        {
            if (!waitingForNextWave)
            {
                waitingForNextWave = true;
                nextWaveStartTime = Time.time + timeBetweenWaves;
                GameManager.ReportWaveState(false);
                SpawnHealthPickup();
            }

            if (Time.time >= nextWaveStartTime)
            {
                isSpawning = true;
                StartCoroutine(StartNextWave());
            }
        }
        else
        {
            waitingForNextWave = false;
            GameManager.ReportWaveState(true);
        }
    }

    private IEnumerator StartNextWave()
    {
        currentWave++;
        PrepareAmmoDropsForWave();

        GameManager.ReportWave(currentWave);
        GameManager.ReportWaveState(true);

        yield return new WaitForSeconds(currentWave == 1 ? 1f : 2f);

        float spawnInterval = Mathf.Max(0.2f, baseSpawnRate - (currentWave * 0.15f));

        yield return SpawnGroup(enemyPrefab, GetWalkerCount(), spawnInterval);

        if (currentWave >= 2)
        {
            yield return SpawnGroup(dasherPrefab, GetDasherCount(), spawnInterval);
        }

        if (currentWave >= 3)
        {
            yield return SpawnGroup(spitterPrefab, GetSpitterCount(), spawnInterval);
        }

        if (currentWave >= 5)
        {
            yield return SpawnGroup(exploderPrefab, GetExploderCount(), spawnInterval);
        }

        if (currentWave % 4 == 0)
        {
            yield return SpawnGroup(bossPrefab, GetBossCount(), 1.2f);
        }

        isSpawning = false;
        waitingForNextWave = false;
    }

    public void TryDropAmmo(Vector3 position)
    {
        if (!HasAnyLimitedAmmoUnlocked())
        {
            return;
        }

        enemiesKilledThisWave++;

        int remainingEnemies = Mathf.Max(0, enemiesInWave - enemiesKilledThisWave);
        int neededDrops = guaranteedAmmoPacks - ammoPacksDropped;
        bool forcedDrop = neededDrops > 0 && remainingEnemies < neededDrops;
        bool randomDrop = Random.value <= ammoDropChance;

        if (!forcedDrop && !randomDrop)
        {
            return;
        }

        SpawnAmmoPickup(position);
    }

    private IEnumerator SpawnGroup(GameObject prefab, int amount, float interval)
    {
        if (prefab == null || amount <= 0)
        {
            yield break;
        }

        for (int i = 0; i < amount; i++)
        {
            SpawnEnemy(prefab);
            yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (player == null || prefab == null) return;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = new Vector3(
            player.position.x + (randomDir.x * randomDist),
            player.position.y + (randomDir.y * randomDist),
            0f
        );

        GameObject spawnedEnemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyAI enemy = spawnedEnemy.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.ConfigureForWave(currentWave);
        }
    }

    private void PrepareAmmoDropsForWave()
    {
        enemiesKilledThisWave = 0;
        ammoPacksDropped = 0;
        enemiesInWave = GetWalkerCount() + GetDasherCount() + GetSpitterCount() + GetExploderCount() + GetBossCount();
        guaranteedAmmoPacks = CalculateGuaranteedAmmoPacks();
    }

    private int CalculateGuaranteedAmmoPacks()
    {
        if (!HasAnyLimitedAmmoUnlocked())
        {
            return 0;
        }

        int totalWaveHp = CalculateTotalWaveHp();
        float targetCoverage = currentWave % 4 == 0 ? bossWaveAmmoCoverage : normalWaveAmmoCoverage;
        float averagePackDamage = GetAveragePackDamage();

        int packs = Mathf.FloorToInt((totalWaveHp * targetCoverage) / averagePackDamage);
        if (enemiesInWave >= 8)
        {
            packs = Mathf.Max(1, packs);
        }

        int maxPacks = currentWave % 4 == 0 ? 10 : 6;
        return Mathf.Clamp(packs, 0, maxPacks);
    }

    private int CalculateTotalWaveHp()
    {
        return
            GetWalkerCount() * GetExpectedEnemyHealth(50, 1f) +
            GetDasherCount() * GetExpectedEnemyHealth(40, 0.8f) +
            GetSpitterCount() * GetExpectedEnemyHealth(30, 0.7f) +
            GetExploderCount() * GetExpectedEnemyHealth(20, 0.4f) +
            GetBossCount() * GetExpectedEnemyHealth(1000, 1f);
    }

    private int GetExpectedEnemyHealth(int baseHealth, float typeMultiplier)
    {
        float waveScale = 1f + ((currentWave - 1) * 0.22f);
        int scaledHealth = Mathf.Max(1, Mathf.RoundToInt(baseHealth * waveScale));
        return Mathf.Max(1, Mathf.RoundToInt(scaledHealth * typeMultiplier));
    }

    private float GetAveragePackDamage()
    {
        float bulletPackDamage = BulletPackAmount * AverageBulletDamage * HitAccuracy;
        float shellPackDamage = ShellPackAmount * AverageShellShotDamage * HitAccuracy;

        if (IsShellAmmoUnlocked())
        {
            return (bulletPackDamage * 0.7f) + (shellPackDamage * 0.3f);
        }

        return bulletPackDamage;
    }

    private void SpawnAmmoPickup(Vector3 position)
    {
        WeaponDefinition.AmmoType ammoType = ChooseAmmoDropType();
        GameObject prefab = ammoType == WeaponDefinition.AmmoType.Shells ? shellAmmoPickupPrefab : bulletAmmoPickupPrefab;

        if (prefab == null)
        {
            return;
        }

        GameObject pickupObject = Instantiate(prefab, position, Quaternion.identity);
        AmmoPickup pickup = pickupObject.GetComponent<AmmoPickup>();
        if (pickup != null)
        {
            int amount = ammoType == WeaponDefinition.AmmoType.Shells ? ShellPackAmount : BulletPackAmount;
            pickup.Setup(ammoType, amount);
        }

        ammoPacksDropped++;
    }

    private WeaponDefinition.AmmoType ChooseAmmoDropType()
    {
        if (IsShellAmmoUnlocked() && Random.value < 0.3f)
        {
            return WeaponDefinition.AmmoType.Shells;
        }

        return WeaponDefinition.AmmoType.Bullets;
    }

    private bool HasAnyLimitedAmmoUnlocked()
    {
        if (playerWeaponController == null)
        {
            ResolvePlayer();
        }

        return playerWeaponController != null &&
            (playerWeaponController.IsAmmoTypeUnlocked(WeaponDefinition.AmmoType.Bullets) ||
             playerWeaponController.IsAmmoTypeUnlocked(WeaponDefinition.AmmoType.Shells));
    }

    private bool IsShellAmmoUnlocked()
    {
        return playerWeaponController != null && playerWeaponController.IsAmmoTypeUnlocked(WeaponDefinition.AmmoType.Shells);
    }

    private int GetWalkerCount()
    {
        return 5 + (currentWave * 2);
    }

    private int GetDasherCount()
    {
        return currentWave >= 2 ? currentWave / 2 : 0;
    }

    private int GetSpitterCount()
    {
        return currentWave >= 3 ? currentWave / 3 : 0;
    }

    private int GetExploderCount()
    {
        return currentWave >= 5 ? currentWave / 4 : 0;
    }

    private int GetBossCount()
    {
        return currentWave % 4 == 0 ? currentWave / 4 : 0;
    }

    private void SpawnHealthPickup()
    {
        if (healthPickupPrefab == null || healthPickupPoint == null || currentHealthPickup != null)
        {
            return;
        }

        currentHealthPickup = Instantiate(healthPickupPrefab, healthPickupPoint.position, healthPickupPoint.rotation);
    }

    private void ResolvePlayer()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null && playerWeaponController == null)
        {
            playerWeaponController = player.GetComponent<WeaponController>();
        }
    }
}
