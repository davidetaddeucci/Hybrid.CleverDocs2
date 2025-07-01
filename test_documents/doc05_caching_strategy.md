# Caching Strategy

## Multi-Level Caching Architecture
The system implements a sophisticated three-tier caching strategy for optimal performance.

### L1 Cache - In-Memory
- **Technology**: .NET MemoryCache
- **Scope**: Per-application instance
- **TTL**: 5-15 minutes
- **Use Cases**: Frequently accessed user data, session info

### L2 Cache - Distributed Redis
- **Technology**: Redis on 192.168.1.4:6380
- **Scope**: Shared across all instances
- **TTL**: 30 minutes to 2 hours
- **Use Cases**: Search results, document metadata, API responses

### L3 Cache - Persistent
- **Technology**: File-based with database fallback
- **Scope**: Long-term storage
- **TTL**: 24 hours to 7 days
- **Use Cases**: Static content, configuration data

### Cache Invalidation
- **Tag-based**: Hierarchical invalidation by entity type
- **Event-driven**: SignalR notifications for real-time updates
- **Time-based**: Automatic expiration with sliding windows
- **Manual**: Admin tools for cache management

### Performance Metrics
- Cache hit ratio: >85% target
- Response time improvement: 60-80%
- Memory usage: <500MB per instance
- Redis memory: <2GB for typical workload

### Cache Keys Strategy
```
user:{userId}:profile
collection:{collectionId}:documents
search:{query_hash}:results:{page}
company:{companyId}:settings
```

The caching strategy ensures sub-second response times while maintaining data consistency across the distributed system.