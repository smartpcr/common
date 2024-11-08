# cache

## Description

This package provides a simple cache interface that can be used to store and retrieve data from a cache.

there are two layers of cache:
  1) local layer with in-memory and fallback to file storage;
  2) distributed cache using redis, blob, or cosmos etc.

## Usage

- when there is secret and value is not provided in settings, it has dependency on keyvault
- distributed cache type can be set to Redis, Blob, Cosmos etc.

```json
"CacheSettings": {
    "TimeToLive": "15.00:00:00",
    "Local": {
        "MemoryCache": {
            "CompactionPercentage": 0.1,
            "SizeLimit": 256
        },
        "FileCache": {
            "CacheFolder": "cache"
        }
    },
    "Distributed": {
        "DistributedCacheType": "Redis",
        "RedisCache": {
            "Endpoint": "localhost:6379",
            "AccessKey": {
                "VaultSecretName": "rediscache-accesskey",
                "Value": ""
            },
            "ProtectionCert": {
                "VaultSecretName": "rediscache-protectioncert",
                "Value": ""
            }
        }
    }
}
```
