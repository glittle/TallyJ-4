import localforage from "localforage";

interface CacheEntry {
  data: any;
  timestamp: number;
  ttl: number; // Time to live in milliseconds
}

class CacheService {
  private store: LocalForage;

  constructor() {
    this.store = localforage.createInstance({
      name: "tallyj-cache",
      storeName: "api-cache",
    });
  }

  async get<T>(key: string): Promise<T | null> {
    try {
      const entry: CacheEntry | null = await this.store.getItem(key);
      if (!entry) {
        return null;
      }

      const now = Date.now();
      if (now - entry.timestamp > entry.ttl) {
        await this.store.removeItem(key);
        return null;
      }

      return entry.data;
    } catch (error) {
      console.warn("Cache get error:", error);
      return null;
    }
  }

  async set(
    key: string,
    data: any,
    ttl: number = 5 * 60 * 1000,
  ): Promise<void> {
    try {
      const entry: CacheEntry = {
        data,
        timestamp: Date.now(),
        ttl,
      };
      await this.store.setItem(key, entry);
    } catch (error) {
      console.warn("Cache set error:", error);
    }
  }

  async remove(key: string): Promise<void> {
    try {
      await this.store.removeItem(key);
    } catch (error) {
      console.warn("Cache remove error:", error);
    }
  }

  async clear(): Promise<void> {
    try {
      await this.store.clear();
    } catch (error) {
      console.warn("Cache clear error:", error);
    }
  }

  // Generate cache key from request config
  generateKey(config: {
    url?: string;
    method?: string;
    params?: any;
    data?: any;
  }): string {
    const { url, method = "GET", params, data } = config;
    const keyParts = [method.toUpperCase(), url];

    if (params && Object.keys(params).length > 0) {
      keyParts.push(JSON.stringify(params));
    }

    if (data && method.toUpperCase() !== "GET") {
      keyParts.push(JSON.stringify(data));
    }

    return keyParts.join("|");
  }
}

export const cacheService = new CacheService();
