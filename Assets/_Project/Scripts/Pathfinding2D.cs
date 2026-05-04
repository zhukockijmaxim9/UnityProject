using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// A* Pathfinding для 2D тайлмап-игры.
/// Считывает Tilemap стен и находит путь в обход препятствий.
/// Вешается на любой объект на сцене (например, на Grid или отдельный пустой объект).
/// </summary>
public class Pathfinding2D : MonoBehaviour
{
    public static Pathfinding2D Instance { get; private set; }

    [Header("Tilemap Settings")]
    [Tooltip("Перетащи сюда Tilemap объект Walls")]
    [SerializeField] private Tilemap wallsTilemap;

    [Header("Grid Settings")]
    [Tooltip("Размер сетки поиска пути (в тайлах от центра). Чем больше, тем дальше зомби могут искать путь, но тяжелее для производительности.")]
    [SerializeField] private int gridHalfWidth = 50;
    [SerializeField] private int gridHalfHeight = 50;

    [Header("Wall Avoidance")]
    [Tooltip("Дополнительная стоимость движения рядом со стеной. Чем выше, тем дальше зомби обходят углы.")]
    [SerializeField] private float wallProximityCost = 3f;

    [Header("Performance")]
    [Tooltip("Максимальное количество итераций A* (защита от зависания)")]
    [SerializeField] private int maxIterations = 2000;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Проверяет, является ли позиция в мире проходимой (нет тайла стены).
    /// </summary>
    public bool IsWalkable(Vector2 worldPos)
    {
        if (wallsTilemap == null) return true;
        Vector3Int cellPos = wallsTilemap.WorldToCell(worldPos);
        return !wallsTilemap.HasTile(cellPos);
    }

    /// <summary>
    /// Находит путь от startWorld до endWorld, обходя стены.
    /// Возвращает список точек в мировых координатах.
    /// Если путь не найден — возвращает null.
    /// </summary>
    public List<Vector2> FindPath(Vector2 startWorld, Vector2 endWorld)
    {
        if (wallsTilemap == null)
        {
            // Нет тайлмапа стен — возвращаем прямой путь
            return new List<Vector2> { startWorld, endWorld };
        }

        Vector3Int startCell = wallsTilemap.WorldToCell(startWorld);
        Vector3Int endCell = wallsTilemap.WorldToCell(endWorld);

        // Если старт или конец внутри стены — пробуем найти ближайшую свободную клетку
        if (wallsTilemap.HasTile(startCell))
        {
            startCell = FindNearestWalkableCell(startCell);
        }
        if (wallsTilemap.HasTile(endCell))
        {
            endCell = FindNearestWalkableCell(endCell);
        }

        // A* алгоритм
        var openSet = new SortedSet<Node>(new NodeComparer());
        var closedSet = new HashSet<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, float>();

        float hStart = Heuristic(startCell, endCell);
        var startNode = new Node(startCell, 0f, hStart);
        openSet.Add(startNode);
        gScore[startCell] = 0f;

        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Берём узел с наименьшей стоимостью
            Node current;
            using (var enumerator = openSet.GetEnumerator())
            {
                enumerator.MoveNext();
                current = enumerator.Current;
            }
            openSet.Remove(current);

            if (current.Position == endCell)
            {
                return ReconstructPath(cameFrom, endCell);
            }

            closedSet.Add(current.Position);

            // Проверяем 8 соседей (включая диагонали)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    Vector3Int neighbor = new Vector3Int(
                        current.Position.x + dx,
                        current.Position.y + dy,
                        0
                    );

                    if (closedSet.Contains(neighbor)) continue;

                    // Проверяем границы сетки
                    if (Mathf.Abs(neighbor.x - startCell.x) > gridHalfWidth ||
                        Mathf.Abs(neighbor.y - startCell.y) > gridHalfHeight)
                        continue;

                    // Проверяем проходимость
                    if (wallsTilemap.HasTile(neighbor)) continue;

                    // Для диагоналей: блокируем если хотя бы одна сторона — стена
                    if (dx != 0 && dy != 0)
                    {
                        bool sideX = wallsTilemap.HasTile(new Vector3Int(current.Position.x + dx, current.Position.y, 0));
                        bool sideY = wallsTilemap.HasTile(new Vector3Int(current.Position.x, current.Position.y + dy, 0));
                        if (sideX || sideY) continue; // Нельзя срезать угол рядом со стеной
                    }

                    float moveCost = (dx != 0 && dy != 0) ? 1.414f : 1f;

                    // Штраф за близость к стене — зомби предпочтут обходить подальше
                    if (IsAdjacentToWall(neighbor))
                    {
                        moveCost += wallProximityCost;
                    }

                    float tentativeG = gScore[current.Position] + moveCost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        gScore[neighbor] = tentativeG;
                        cameFrom[neighbor] = current.Position;
                        float h = Heuristic(neighbor, endCell);
                        openSet.Add(new Node(neighbor, tentativeG, h));
                    }
                }
            }
        }

        // Путь не найден
        return null;
    }

    private List<Vector2> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var path = new List<Vector2>();
        path.Add(CellToWorld(current));

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(CellToWorld(current));
        }

        path.Reverse();

        // Упрощаем путь — убираем промежуточные точки на прямых линиях
        return SimplifyPath(path);
    }

    private List<Vector2> SimplifyPath(List<Vector2> path)
    {
        if (path.Count <= 2) return path;

        var simplified = new List<Vector2>();
        simplified.Add(path[0]);

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 prev = path[i - 1];
            Vector2 curr = path[i];
            Vector2 next = path[i + 1];

            Vector2 dir1 = (curr - prev).normalized;
            Vector2 dir2 = (next - curr).normalized;

            // Если направление изменилось — это поворот, сохраняем точку
            if (Vector2.Dot(dir1, dir2) < 0.99f)
            {
                simplified.Add(curr);
            }
        }

        simplified.Add(path[path.Count - 1]);
        return simplified;
    }

    private Vector2 CellToWorld(Vector3Int cell)
    {
        // Возвращаем центр клетки
        Vector3 worldPos = wallsTilemap.CellToWorld(cell);
        Vector3 cellSize = wallsTilemap.cellSize;
        return new Vector2(worldPos.x + cellSize.x * 0.5f, worldPos.y + cellSize.y * 0.5f);
    }

    private Vector3Int FindNearestWalkableCell(Vector3Int cell)
    {
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                    Vector3Int check = new Vector3Int(cell.x + dx, cell.y + dy, 0);
                    if (!wallsTilemap.HasTile(check)) return check;
                }
            }
        }
        return cell;
    }

    /// <summary>
    /// Проверяет, есть ли стена в любой из 8 соседних клеток.
    /// </summary>
    private bool IsAdjacentToWall(Vector3Int cell)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (wallsTilemap.HasTile(new Vector3Int(cell.x + dx, cell.y + dy, 0)))
                    return true;
            }
        }
        return false;
    }

    private float Heuristic(Vector3Int a, Vector3Int b)
    {
        // Октильное расстояние (для 8 направлений)
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.414f * Mathf.Min(dx, dy);
    }

    // --- Вспомогательные структуры для A* ---
    private struct Node
    {
        public Vector3Int Position;
        public float G; // Стоимость от старта
        public float F; // G + эвристика

        public Node(Vector3Int pos, float g, float h)
        {
            Position = pos;
            G = g;
            F = g + h;
        }
    }

    private class NodeComparer : IComparer<Node>
    {
        public int Compare(Node a, Node b)
        {
            int result = a.F.CompareTo(b.F);
            if (result == 0) result = a.G.CompareTo(b.G);
            if (result == 0) result = a.Position.x.CompareTo(b.Position.x);
            if (result == 0) result = a.Position.y.CompareTo(b.Position.y);
            return result;
        }
    }
}
