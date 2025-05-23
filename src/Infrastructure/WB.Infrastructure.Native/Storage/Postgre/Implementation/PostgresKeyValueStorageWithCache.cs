﻿using System;
using Microsoft.Extensions.Caching.Memory;
using WB.Core.Infrastructure.PlainStorage;

namespace WB.Infrastructure.Native.Storage.Postgre.Implementation
{
    internal class PostgresKeyValueStorageWithCache<TEntity> :
        PostgresKeyValueStorage<TEntity>,
            IPlainKeyValueStorage<TEntity>
        where TEntity : class
    {
        private readonly IMemoryCache memoryCache;

        private static readonly string CachePrefix = $"pkvs::{typeof(TEntity).Name}::";
        private static readonly object lockObj = new object();

        public PostgresKeyValueStorageWithCache(IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            IEntitySerializer<TEntity> serializer)
            : base(unitOfWork, serializer)
        {
            this.memoryCache = memoryCache;
        }

        public override TEntity GetById(string id)
        {
            var entity = memoryCache.GetOrCreate(CachePrefix + id, cache =>
            {
                lock (lockObj)
                {
                    cache.SlidingExpiration = TimeSpan.FromSeconds(10);
                    return base.GetById(id);
                }
            });
            return entity;
        }

        public override void Remove(string id)
        {
            try
            {
                base.Remove(id);
            }
            finally
            {
                memoryCache.Remove(CachePrefix + id);
            }
        }

        public override void Store(TEntity view, string id)
        {
            try
            {
                base.Store(view, id);
            }
            finally
            {
                memoryCache.Remove(CachePrefix + id);
            }
        }

        public override string GetReadableStatus()
        {
            return "Postgres with Cache K/V :/";
        }
    }
}
