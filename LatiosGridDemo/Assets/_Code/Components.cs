using System;
using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Dragons
{
    public struct GridPosition : IComponentData
    {
        public int2 position;
    }

    public struct GridConfiguration : IComponentData
    {
        public int rows;
        public int cellsPerRow;

        public Entity prefabTile0;
        public Entity prefabTile1;
    }

    public struct PlayerTag : IComponentData { }

    public struct ShouldExecuteTurn : IComponentData
    {
        public bool shouldExecuteTurn;
    }

    public struct CharacterActionDirection : IComponentData
    {
        public int2 direction;
    }

    public struct CharacterWantsToMove : IComponentData
    {
        public bool wantsToMove;
    }

    public struct CharacterWantsToAttack : IComponentData
    {
        public bool wantsToAttack;
    }

    public struct AiFixedAction : IComponentData
    {
        public bool move;
        public bool attack;
        public int2 direction;
    }

    public struct GridTag : IComponentData { }

    public struct Grid : ICollectionComponent
    {
        private NativeArray<Entity> m_array;
        private int                 m_cellsPerRow;

        public Grid(int rows, int cellsPerRow, Allocator allocator)
        {
            m_cellsPerRow = cellsPerRow;
            m_array       = new NativeArray<Entity>(rows * cellsPerRow, allocator);
        }

        public Entity this[int indexInRow, int row]
        {
            get => m_array[row * m_cellsPerRow + indexInRow];
            set => m_array[row * m_cellsPerRow + indexInRow] = value;
        }

        public int2 dimensions => new int2(m_cellsPerRow, m_array.Length / m_cellsPerRow);

        public Type AssociatedComponentType => typeof(GridTag);

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return m_array.Dispose(inputDeps);
        }

        public Parallel AsParallelUnsafe() => new Parallel(this);

        public struct Parallel
        {
            [NativeDisableParallelForRestriction] private NativeArray<Entity> m_array;
            private int                                                       m_cellsPerRow;

            public Entity this[int indexInRow, int row]
            {
                get => m_array[row * m_cellsPerRow + indexInRow];
                set => m_array[row * m_cellsPerRow + indexInRow] = value;
            }

            public int2 dimensions => new int2(m_cellsPerRow, m_array.Length / m_cellsPerRow);

            public Parallel(Grid grid)
            {
                m_array       = grid.m_array;
                m_cellsPerRow = grid.m_cellsPerRow;
            }
        }
    }

    public struct Followed : IComponentData
    {
        public Entity follower;
    }
}

