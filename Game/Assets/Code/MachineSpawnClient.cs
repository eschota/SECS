using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

/// <summary>
/// НИЧЕГО не делает сам. Спавнит машину ТОЛЬКО по явному вызову RequestSpawnFromCreator().
/// Точка спавна считается локально от персонального якоря по PlayerId,
/// затем подбирается свободная точка вокруг якоря по AABB на плоскости XZ,
/// учитывая текущие машины и «ауры» PlayerPrefab.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class MachineSpawnClient : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private NetworkObject machinePrefab; // префаб с NetworkObject + NetworkTransform + Rigidbody + Machine
    [SerializeField] private Creator creator;             // если не назначен — найдём в сцене

    [Header("Spawn search (local)")]
    [SerializeField] private float anchorRadius      = 30f;
    [SerializeField] private int   maxSpiralTries    = 80;
    [SerializeField] private float spiralStep        = 4f;
    [SerializeField] private float avoidOriginRadius = 10f;
    [SerializeField] private float playerAuraPadding = 5f;

    private const string TAG = "<color=#FFA726>[Spawn]</color>";

    public override void Spawned()
    {
        Debug.Log($"{TAG} Spawned() called, HasInputAuthority: {Object?.HasInputAuthority}");
        
        // этот компонент должен работать только у владельца PlayerPrefab
        if (!Object.HasInputAuthority) 
        { 
            Debug.Log($"{TAG} No InputAuthority, disabling component");
            enabled = false; 
            return; 
        }
        
        Debug.Log($"{TAG} Component enabled for local player");
        
        if (!creator) creator = FindFirstObjectByType<Creator>();
        if (!machinePrefab) Debug.LogWarning($"{TAG} Machine Prefab не назначен на PlayerPrefab!");
        
        Play.OnPlayStateChange += OnPlayStateChangeLocal;
        
        // Уведомляем сервер о подключении нового игрока
        RPC_NotifyPlayerJoined();
        
        Debug.Log($"{TAG} Spawned() completed successfully");
    }

    /// <summary>
    /// Уведомляем сервер о подключении нового игрока
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_NotifyPlayerJoined()
    {
        Debug.Log($"{TAG} New player joined, requesting existing machines");
        
        // Запрашиваем отправку существующих машин новому игроку
        RPC_RequestExistingMachines();
    }

    /// <summary>
    /// Запрашиваем отправку существующих машин
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestExistingMachines()
    {
        Debug.Log($"{TAG} Sending existing machines to new player");
        
        // Находим все существующие машины и отправляем их новому игроку
        var existingMachines = Runner.GetAllBehaviours<Machine>();
        foreach (var machine in existingMachines)
        {
            if (machine != null && machine.Object != null && machine.Object.IsValid)
            {
                // Отправляем информацию о существующей машине новому игроку
                machine.RPC_SendMachineToNewPlayer(Runner.LocalPlayer);
            }
        }
    }

    private void OnDestroy()
    {
        if (Object && Object.HasInputAuthority)
            Play.OnPlayStateChange -= OnPlayStateChangeLocal;
    }

    private void OnPlayStateChangeLocal(Play.State st)
    {
        if (!creator) creator = FindFirstObjectByType<Creator>();
        if (!creator) return;

        // вернулись к сборке — показать обратно
        if (st == Play.State.Create) SetConstructionHidden(false);
    }

    /// <summary>
    /// Явный вызов из Play после перехода в SimulateOnline.
    /// </summary>
    public void RequestSpawnFromCreator()
    {
        Debug.Log($"{TAG} RequestSpawnFromCreator called");
        Debug.Log($"{TAG} Runner: {Runner}, IsRunning: {Runner?.IsRunning}, HasInputAuthority: {Object?.HasInputAuthority}");
        
        if (!Runner || !Runner.IsRunning || !Object.HasInputAuthority)
        {
            Debug.LogWarning($"{TAG} Runner не готов или нет InputAuthority.");
            return;
        }
        if (!creator || creator.cells == null || creator.cells.Count == 0)
        {
            Debug.LogWarning($"{TAG} Нет построенных клеток — спавн отменён.");
            return;
        }
        if (Runner.GetAllBehaviours<Machine>().Any(m => m && m.Object.InputAuthority == Runner.LocalPlayer))
        {
            Debug.Log($"{TAG} У локального игрока уже есть машина — пропуск.");
            return;
        }

        // 1) Собираем blueprint и оцениваем радиус
        var bp    = BuildBlueprintFromCreator(creator);
        var bytes = BlueprintCodec.ToBytesOptimized(bp);
        
        float newRadius = EstimateHorizontalRadius(bp);

        // 2) Персональный якорь и поиск свободной точки
        Vector3 anchor = GetSpawnAnchor(Runner.LocalPlayer, anchorRadius);
        if (new Vector2(anchor.x, anchor.z).sqrMagnitude < avoidOriginRadius * avoidOriginRadius)
            anchor = PushFromOrigin(anchor, avoidOriginRadius);

        var occupied = CollectOccupiedSpotsLocal(newRadius);
        float angleStart = (Runner.LocalPlayer.PlayerId % 24) * (Mathf.PI * 2f / 24f);
        Vector3 spawnPos = FindFreeSpotXZ(anchor, occupied, maxSpiralTries, spiralStep, newRadius, angleStart);
        if (new Vector2(spawnPos.x, spawnPos.z).sqrMagnitude < 1f)
            spawnPos = PushFromOrigin(anchor, avoidOriginRadius);

        // 3) Спавним локально (в Shared это нормально) и сразу шлём blueprint всем
        var spawned = Runner.Spawn(machinePrefab, spawnPos, Quaternion.identity, Runner.LocalPlayer);
        if (spawned == null)
        {
            Debug.LogError($"{TAG} Spawn вернул NULL. Проверь NetworkProjectConfig и Prefab.");
            return;
        }
        spawned.name = $"Machine_P{Runner.LocalPlayer.PlayerId}";

        var machine = spawned.GetComponent<Machine>();
        if (!machine)
        {
            Debug.LogError($"{TAG} На префабе Machine нет компонента Machine!");
            return;
        }

        // Проверяем размер данных и отправляем соответствующим способом
        Debug.Log($"{TAG} Blueprint size check: {bytes.Length} bytes, safe limit: 480 bytes, condition: {bytes.Length > 480}");
        if (bytes.Length > 480) // Учитываем накладные расходы Fusion (~32 байта)
        {
            Debug.LogWarning($"{TAG} Blueprint data size ({bytes.Length} bytes) exceeds RPC limit. Using chunked transmission.");
            var chunks = BlueprintCodec.SplitBlueprintForRPC(bp);
            
            // Проверяем размер каждого чанка
            for (int i = 0; i < chunks.Count; i++)
            {
                if (chunks[i].Length > 512)
                {
                    Debug.LogError($"{TAG} Chunk {i} is still too large: {chunks[i].Length} bytes!");
                }
                else
                {
                    Debug.Log($"{TAG} Chunk {i} size: {chunks[i].Length} bytes");
                }
            }
            
            SendBlueprintChunked(machine, chunks);
        }
        else
        {
            Debug.Log($"{TAG} Using single RPC transmission for {bytes.Length} bytes (safe under 480 bytes limit)");
            machine.RPC_SetBlueprint(bytes);
            Debug.Log($"{TAG} Spawned NO={spawned.Id} at {spawnPos}  cells={bp.cells.Count}  bytes={bytes.Length}");
        }

        // 4) Прячем СВОЙ конструктор
        SetConstructionHidden(true);
    }
public void RequestDespawnOwnedMachine()
{
    const string TAG = "<color=#FFA726>[Spawn]</color>";

    // 0) сам объект жив?
    if (!this || !gameObject)
        return;

    // 1) Runner гарантированно есть и запущен?
    var runner = Runner;
    if (runner == null || !runner.IsRunning)
    {
        Debug.LogWarning($"{TAG} Runner == null или не запущен — despawn пропущен.");
        return;
    }

    // 2) Ищем СВОЮ машину (InputAuthority == LocalPlayer) среди сетевых объектов
    Machine mine = null;
    foreach (var m in runner.GetAllBehaviours<Machine>())
    {
        if (m == null) continue;
        var no = m.Object;
        if (no == null || !no.IsValid) continue;
        if (no.InputAuthority == runner.LocalPlayer) { mine = m; break; }
    }

    // 3) На всякий случай — fallback через поиск в сцене
    if (mine == null)
    {
        foreach (var m in FindObjectsOfType<Machine>())
        {
            if (m == null) continue;
            var no = m.GetComponent<NetworkObject>();
            if (no != null && no.IsValid && no.InputAuthority == runner.LocalPlayer)
            { mine = m; break; }
        }
    }

    if (mine == null)
    {
        Debug.Log($"{TAG} Своей машины не найдено — ничего удалять.");
        return;
    }

    // 4) Безопасный despawn
    if (mine.Object != null && mine.Object.IsValid)
    {
        Debug.Log($"{TAG} Despawn NO={mine.Object.Id} ({mine.name})");
        runner.Despawn(mine.Object);
    }
    else
    {
        Debug.LogWarning($"{TAG} NetworkObject недействителен — локально уничтожаю GameObject {mine.name}");
        Destroy(mine.gameObject);
    }
}

    // ---------- занятые зоны (локальный снимок) ----------
    private List<(Vector2 pos, float radius)> CollectOccupiedSpotsLocal(float newRadius)
    {
        var list = new List<(Vector2, float)>();

        // Машины
        foreach (var m in Runner.GetAllBehaviours<Machine>())
        {
            if (!m || !m.gameObject.activeInHierarchy) continue;
            float r;
            if (m.WorldBounds.size != Vector3.zero)
            {
                var ex = m.WorldBounds.extents;
                r = Mathf.Sqrt(ex.x * ex.x + ex.z * ex.z);
            }
            else r = Mathf.Max(0.75f, m.safeRadius * 0.5f);

            list.Add((new Vector2(m.transform.position.x, m.transform.position.z), r));
        }

        // Ауры игроков
        foreach (var pc in FindObjectsOfType<PlayerController>())
        {
            var pos = pc.transform.position;
            list.Add((new Vector2(pos.x, pos.z), Mathf.Max(1f, playerAuraPadding + newRadius)));
        }

        return list;
    }

    // ---------- геометрия/поиск ----------
    private static Vector3 GetSpawnAnchor(PlayerRef player, float R)
    {
        const float GOLDEN_DEG = 137.50776405f;
        float angle = Mathf.Deg2Rad * (player.PlayerId * GOLDEN_DEG);
        return new Vector3(Mathf.Cos(angle) * R, 1f, Mathf.Sin(angle) * R);
    }

    private static Vector3 PushFromOrigin(Vector3 p, float minRadius)
    {
        Vector2 v = new Vector2(p.x, p.z);
        if (v.sqrMagnitude < minRadius * minRadius)
        {
            if (v.sqrMagnitude < 0.001f) v = new Vector2(minRadius, 0);
            v = v.normalized * minRadius;
            return new Vector3(v.x, p.y, v.y);
        }
        return p;
    }

    private static Vector3 FindFreeSpotXZ(
        Vector3 anchor,
        List<(Vector2 pos, float radius)> occupied,
        int tries, float step,
        float newRadius,
        float angleStart)
    {
        float angle = angleStart;
        float radius = 0f;

        for (int i = 0; i < tries; i++)
        {
            Vector3 p = anchor + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            bool ok = true;
            var a = new Vector2(p.x, p.z);

            if (a.sqrMagnitude < 1f) ok = false;
            if (ok)
            {
                for (int s = 0; s < occupied.Count; s++)
                {
                    float need = 2f * (occupied[s].radius + newRadius);
                    if ((a - occupied[s].pos).sqrMagnitude < need * need) { ok = false; break; }
                }
            }

            if (ok) { p.y = 1f; return p; }
            angle += Mathf.PI / 4f;
            if ((i & 1) == 1) radius += step;
        }
        var fallback = anchor; fallback.y = 1f; return fallback;
    }

    // ---------- blueprint / утилиты ----------
    private static MachineBlueprint BuildBlueprintFromCreator(Creator cr)
    {
        var bp = new MachineBlueprint();
        Debug.Log($"[Spawn] Building blueprint from {cr.cells.Count} cells");
        Debug.Log($"[Spawn] Creator.prefabLookup contains {cr.prefabLookup.Count} prefabs");
        
        // Выводим доступные префабы для отладки
        if (cr.prefabLookup.Count > 0)
        {
            string availablePrefabs = string.Join(", ", cr.prefabLookup.Keys.Take(5));
            Debug.Log($"[Spawn] Available prefabs: {availablePrefabs}");
        }
        
        foreach (var cell in cr.cells)
        {
            if (!cell) continue;
            if (cell.Status == io_base.io_base_status.Creating) continue;
            if (cell.Status == io_base.io_base_status.Hidden) continue;

            // Используем полиморфную сериализацию
            io_base_serialized cellData = CreateSerializedData(cell);
            bp.cells.Add(cellData);
            
            Debug.Log($"[Spawn] Adding cell: {cell.name}, type: {cell.GetCellType()}, position: {cell.target_world_position}");
        }
        
        Debug.Log($"[Spawn] Blueprint created with {bp.cells.Count} cells");
        return bp;
    }
    
    private static io_base_serialized CreateSerializedData(io_base cell)
    {
        // Создаем правильный тип данных на основе типа клетки
        io_base_serialized cellData;
        
        switch (cell.GetCellType())
        {
            case "io_engine":
                cellData = new io_engine_serialized();
                break;
            default:
                cellData = new io_base_serialized();
                break;
        }
        
        // Используем полиморфную сериализацию
        cell.SerializeToData(cellData);
        return cellData;
    }

    private static float EstimateHorizontalRadius(MachineBlueprint bp)
    {
        if (bp == null || bp.cells.Count == 0) return 0.75f;

        float minX =  float.MaxValue, minZ =  float.MaxValue;
        float maxX = -float.MaxValue, maxZ = -float.MaxValue;

        foreach (var c in bp.cells)
        {
            if (c._target_world_position.x < minX) minX = c._target_world_position.x;
            if (c._target_world_position.x > maxX) maxX = c._target_world_position.x;
            if (c._target_world_position.z < minZ) minZ = c._target_world_position.z;
            if (c._target_world_position.z > maxZ) maxZ = c._target_world_position.z;
        }

        float dx = (maxX - minX) + 1.0f; // + диаметр клетки
        float dz = (maxZ - minZ) + 1.0f;
        float halfDiag = 0.5f * Mathf.Sqrt(dx * dx + dz * dz);

        return Mathf.Max(0.75f, halfDiag);
    }

    private void SetConstructionHidden(bool hidden)
    {
        if (!creator) return;
        foreach (var cell in creator.cells)
        {
            if (!cell) continue;
            foreach (var r in cell.GetComponentsInChildren<Renderer>(true)) r.enabled = !hidden;
            foreach (var c in cell.GetComponentsInChildren<Collider>(true)) c.enabled = !hidden;
        }
    }

    /// <summary>
    /// Отправляет blueprint по частям через RPC
    /// </summary>
    private void SendBlueprintChunked(Machine machine, List<byte[]> chunks)
    {
        Debug.Log($"{TAG} Отправляем blueprint по частям: {chunks.Count} чанков");
        
        // Начинаем передачу
        machine.RPC_StartBlueprintChunked(chunks.Count);
        
        // Отправляем каждый чанк
        for (int i = 0; i < chunks.Count; i++)
        {
            machine.RPC_ReceiveBlueprintChunk(i, chunks[i]);
        }
        
        Debug.Log($"{TAG} Blueprint отправлен по частям успешно");
    }
}
