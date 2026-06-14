using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;

namespace GastronomePlatform.Modules.Dishes.Application.Helpers
{
    /// <summary>
    /// Утилиты для проверки инвариантов иерархии категорий (UC-DSH-101 / UC-DSH-104):
    /// глубина не более <see cref="Category.MAX_DEPTH"/>, отсутствие циклов в
    /// <see cref="Category.ParentId"/>.
    /// </summary>
    /// <remarks>
    /// Принимает плоский список всех категорий из <c>ICategoryRepository.ListAllAsync</c>
    /// (с неактивными узлами — admin-операции учитывают всё дерево). Логика —
    /// in-memory обходы по <see cref="Category.ParentId"/> за O(depth).
    /// </remarks>
    public static class CategoryHierarchyValidator
    {
        /// <summary>
        /// Считает глубину родительской цепочки от <paramref name="parentId"/> до корня.
        /// Корневая категория имеет глубину 1, её прямой потомок — 2, и так далее.
        /// </summary>
        /// <param name="parentId">Идентификатор родителя или <see langword="null"/> для проверки корневого узла.</param>
        /// <param name="byId">Словарь категорий по идентификатору.</param>
        /// <returns>
        /// Глубина уровня, который займёт ребёнок указанного родителя. Для нового
        /// корня (<paramref name="parentId"/> = <see langword="null"/>) — 1.
        /// </returns>
        public static int CalculateChildDepth(Guid? parentId, IReadOnlyDictionary<Guid, Category> byId)
        {
            if (!parentId.HasValue)
            {
                return 1;
            }

            int depth = 1;
            Guid? current = parentId;
            // Защитный потолок цикла на случай битых данных — больше, чем MAX_DEPTH.
            int safetyLimit = Category.MAX_DEPTH * 4;
            int steps = 0;
            while (current.HasValue && steps < safetyLimit)
            {
                if (!byId.TryGetValue(current.Value, out Category? node))
                {
                    break;
                }

                depth++;
                current = node.ParentId;
                steps++;
            }

            return depth;
        }

        /// <summary>
        /// Проверяет, что добавление дочернего узла к указанному родителю не превысит
        /// <see cref="Category.MAX_DEPTH"/>. Если глубина превышена — возвращает
        /// <see cref="DishesErrors.CategoryDepthExceeded"/>.
        /// </summary>
        /// <param name="parentId">Идентификатор будущего родителя.</param>
        /// <param name="byId">Словарь категорий по идентификатору.</param>
        /// <returns>Успех или ошибка превышения глубины.</returns>
        public static Result EnsureChildDepthWithinLimit(
            Guid? parentId,
            IReadOnlyDictionary<Guid, Category> byId)
        {
            int childDepth = CalculateChildDepth(parentId, byId);
            return childDepth > Category.MAX_DEPTH
                ? DishesErrors.CategoryDepthExceeded
                : Result.Success();
        }

        /// <summary>
        /// Возвращает множество идентификаторов всех потомков указанной категории
        /// (рекурсивно). Используется в UC-DSH-104 MoveCategory для предотвращения
        /// перемещения категории в её же поддерево.
        /// </summary>
        /// <param name="categoryId">Идентификатор корневой категории поддерева.</param>
        /// <param name="all">Полный плоский список категорий справочника.</param>
        /// <returns>Множество идентификаторов потомков; <paramref name="categoryId"/> в результат не входит.</returns>
        public static HashSet<Guid> CollectDescendants(Guid categoryId, IReadOnlyList<Category> all)
        {
            // Группируем по ParentId для O(1) lookup детей.
            Dictionary<Guid, List<Category>> childrenByParent = new();
            foreach (Category c in all)
            {
                if (!c.ParentId.HasValue)
                {
                    continue;
                }
                if (!childrenByParent.TryGetValue(c.ParentId.Value, out List<Category>? list))
                {
                    list = new List<Category>();
                    childrenByParent[c.ParentId.Value] = list;
                }
                list.Add(c);
            }

            HashSet<Guid> descendants = new();
            Stack<Guid> stack = new();
            stack.Push(categoryId);

            while (stack.Count > 0)
            {
                Guid current = stack.Pop();
                if (!childrenByParent.TryGetValue(current, out List<Category>? children))
                {
                    continue;
                }

                foreach (Category child in children)
                {
                    if (descendants.Add(child.Id))
                    {
                        stack.Push(child.Id);
                    }
                }
            }

            return descendants;
        }

        /// <summary>
        /// Считает максимальную глубину поддерева с корнем в указанной категории
        /// (включая саму категорию). Используется в UC-DSH-104 для оценки, какую
        /// глубину займёт перемещаемое поддерево.
        /// </summary>
        /// <param name="rootId">Идентификатор корня поддерева.</param>
        /// <param name="all">Полный плоский список категорий.</param>
        /// <returns>Глубина поддерева в количестве уровней (≥ 1).</returns>
        public static int CalculateSubtreeDepth(Guid rootId, IReadOnlyList<Category> all)
        {
            Dictionary<Guid, List<Category>> childrenByParent = new();
            foreach (Category c in all)
            {
                if (!c.ParentId.HasValue)
                {
                    continue;
                }
                if (!childrenByParent.TryGetValue(c.ParentId.Value, out List<Category>? list))
                {
                    list = new List<Category>();
                    childrenByParent[c.ParentId.Value] = list;
                }
                list.Add(c);
            }

            return Walk(rootId, childrenByParent);

            static int Walk(Guid nodeId, IReadOnlyDictionary<Guid, List<Category>> map)
            {
                if (!map.TryGetValue(nodeId, out List<Category>? children) || children.Count == 0)
                {
                    return 1;
                }

                int max = 0;
                foreach (Category c in children)
                {
                    int sub = Walk(c.Id, map);
                    if (sub > max)
                    {
                        max = sub;
                    }
                }
                return 1 + max;
            }
        }
    }
}
