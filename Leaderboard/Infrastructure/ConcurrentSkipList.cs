using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Leaderboard.Infrastructure;


/// <summary>
/// 跳表实现
/// 跳表是一种随机化的数据结构，通过维护多层链表来实现O(log n)的查找效率
/// 每一层都是下一层的子集，最底层包含所有元素
/// </summary>
/// <typeparam name="T">跳表存储的数据类型，必须实现IComparable接口</typeparam>
public class ConcurrentSkipList<T> :ICollection<T> where T : IComparable<T>
{
    public class SkipListNode<T>
    {
        public T Value { get; }

        /// <summary>
        /// 每层的下一个节点指针数组
        /// Next[i] 表示第i层的下一个节点
        /// </summary>
        public SkipListNode<T>[] Next { get; }

        /// <summary>
        /// 每层的跨度数组
        /// Span[i] 表示从当前节点到Next[i]节点之间跨越的节点数（不包括Next[i]）
        /// 例如：如果当前节点在第0层的下一个节点是第3个节点，则Span[0] = 2
        /// </summary>
        public int[] Span { get; }

        public SkipListNode(T value, int level)
        {
            Value = value;
            Next = new SkipListNode<T>[level];
            Span = new int[level];
        }
    }
    /// <summary>
    /// 头节点，不存储实际数据，作为每层链表的起点
    /// </summary>
    private readonly SkipListNode<T> _head;

    /// <summary>
    /// 随机数生成器，用于决定新节点的层数
    /// </summary>
    private readonly Random _random;

    /// <summary>
    /// 当前跳表的最大层数
    /// </summary>
    private int _maxLevel;

    /// <summary>
    /// 跳表中的元素数量
    /// </summary>
    private int _count;

    /// <summary>
    /// 跳表的最大层数限制
    /// </summary>
    private const int MaxLevel = 32;

    /// <summary>
    /// 用于决定是否增加层数的概率
    /// </summary>
    private const double Probability = 0.5;

    /// <summary>
    /// 读写锁，保证线程安全
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    public ConcurrentSkipList()
    {
        _maxLevel = 1;
        _head = new SkipListNode<T>(default, MaxLevel);
        _random = new Random();
        _count = 0;
        // 初始化头节点的每层跨度
        for (int i = 0; i < MaxLevel; i++)
        {
            _head.Span[i] = 0;
        }
    }

    public int Count => _count;

    private int RandomLevel()
    {
        int level = 1;
        while (_random.NextDouble() < Probability && level < MaxLevel)
            level++;
        return level;
    }
    public void Add(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        _lock.EnterWriteLock();
        try
        {
            // update数组记录每层需要更新的节点
            var update = new SkipListNode<T>[MaxLevel];
            // rank数组记录每层经过的节点数
            var rank = new int[MaxLevel];
            var current = _head;

            // 从最高层开始，找到每层需要更新的节点
            for (int i = _maxLevel - 1; i >= 0; i--)
            {
                // 计算当前层经过的节点数
                rank[i] = i == _maxLevel - 1 ? 0 : rank[i + 1];
                // 在当前层找到第一个大于等于value的节点的前一个节点
                while (current.Next[i] != null && current.Next[i].Value.CompareTo(value) < 0)
                {
                    rank[i] += current.Span[i];
                    current = current.Next[i];
                }
                update[i] = current;
            }

            // 随机决定新节点的层数
            int level = RandomLevel();
            // 如果新节点的层数大于当前最大层数，需要更新头节点
            if (level > _maxLevel)
            {
                for (int i = _maxLevel; i < level; i++)
                {
                    update[i] = _head;
                    update[i].Span[i] = _count;
                }
                _maxLevel = level;
            }

            // 创建新节点
            var newNode = new SkipListNode<T>(value, level);
            // 更新每层的指针和跨度
            for (int i = 0; i < level; i++)
            {
                newNode.Next[i] = update[i].Next[i];
                update[i].Next[i] = newNode;
                // 更新跨度：
                // 1. 新节点的跨度 = 原跨度 - (rank[0] - rank[i])
                // 2. 更新节点的跨度 = (rank[0] - rank[i]) + 1
                newNode.Span[i] = update[i].Span[i] - (rank[0] - rank[i]);
                update[i].Span[i] = (rank[0] - rank[i]) + 1;
            }

            // 更新更高层的跨度
            for (int i = level; i < _maxLevel; i++)
            {
                update[i].Span[i]++;
            }
            _count++;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Remove(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        _lock.EnterWriteLock();
        try
        {
            // update数组记录每层需要更新的节点
            var update = new SkipListNode<T>[MaxLevel];
            var current = _head;

            // 从最高层开始，找到每层需要更新的节点
            for (int i = _maxLevel - 1; i >= 0; i--)
            {
                while (current.Next[i] != null && current.Next[i].Value.CompareTo(value) < 0)
                {
                    current = current.Next[i];
                }
                update[i] = current;
            }

            // 检查是否找到要删除的节点
            current = current.Next[0];
            if (current == null || current.Value.CompareTo(value) != 0)
                return false;

            // 更新每层的指针和跨度
            for (int i = 0; i < _maxLevel; i++)
            {
                if (update[i].Next[i] == current)
                {
                    // 如果当前层存在要删除的节点，更新指针和跨度
                    update[i].Span[i] += current.Span[i] - 1;
                    update[i].Next[i] = current.Next[i];
                }
            }

            // 如果最高层变为空，降低最大层数
            while (_maxLevel > 1 && _head.Next[_maxLevel - 1] == null)
                _maxLevel--;
            _count--;
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 获取元素在跳表中的排名（从1开始）
    /// </summary>
    public int GetRank(T value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        _lock.EnterReadLock();
        try
        {
            var current = _head;
            int rank = 0;

            // 从最高层开始，累加经过的节点数
            for (int i = _maxLevel - 1; i >= 0; i--)
            {
                while (current.Next[i] != null && current.Next[i].Value.CompareTo(value) < 0)
                {
                    rank += current.Span[i];
                    current = current.Next[i];
                }
            }

            // 检查是否找到目标节点
            current = current.Next[0];
            if (current != null && current.Value.CompareTo(value) == 0)
            {
                rank++;
                return rank;
            }
            return -1;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 获取指定范围的元素
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="count"></param>
    public List<T> GetRange(int startIndex, int count)
    {
        if (startIndex < 0 || count <= 0)
            return new List<T>();

        _lock.EnterReadLock();
        try
        {
            var result = new List<T>();
            var current = _head;
            int traversed = 0;

            // 从最高层开始，快速定位到起始位置
            for (int i = _maxLevel - 1; i >= 0; i--)
            {
                while (current.Next[i] != null && traversed + current.Span[i] <= startIndex)
                {
                    traversed += current.Span[i];
                    current = current.Next[i];
                }
            }

            // 从起始位置开始，顺序获取指定数量的元素
            current = current.Next[0];
            int collected = 0;
            while (current != null && collected < count)
            {
                result.Add(current.Value);
                current = current.Next[0];
                collected++;
            }
            return result;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            for (int i = 0; i < _maxLevel; i++)
            {
                _head.Next[i] = null;
                _head.Span[i] = 0;
            }
            _maxLevel = 1;
            _count = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Contains(T item)
    {
        return GetRank(item) != -1;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < _count)
            throw new ArgumentException("index out of bounds");
        _lock.EnterReadLock();
        try
        {
            var current = _head.Next[0];
            while (current != null)
            {
                array[arrayIndex++] = current.Value;
                current = current.Next[0];
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsReadOnly => false;

    public IEnumerator<T> GetEnumerator()
    {
        _lock.EnterReadLock();
        try
        {
            var current = _head.Next[0];
            while (current != null)
            {
                yield return current.Value;
                current = current.Next[0];
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
