using System.Timers;
using Newtonsoft.Json;

namespace LibraServer
{
    public struct CacheItem<T> where T : struct
    {
        [JsonIgnore]
        public DateTime CreationTime { get; set; }
        [JsonIgnore]
        public string Name { get; set; }
     
        public T Value { get; set; }
    }
    internal class ItemsCache<T>: IDisposable where T : struct
    {

        readonly List<CacheItem<T>> blockstarsCache = new List<CacheItem<T>>();
        readonly System.Timers.Timer timer = new System.Timers.Timer();
        public ItemsCache()
        {
            timer.Interval = Constants.CACHE_GC_TIME;
            timer.Elapsed += OnTimerTick;
            timer.Start();
        }
        public bool HasItemWithName(string name)
        {
            lock (blockstarsCache)
            {
                return blockstarsCache.Where(x => x.Name == name).Any();
            }
        }
        public void AddItems(string name, params T[] items)
        {
            lock (blockstarsCache)
            {
                foreach (var item in items)
                {
                    blockstarsCache.Add(new CacheItem<T>
                    {
                        CreationTime = DateTime.Now,
                        Name = name,
                        Value = item
                    });
                }
               
            }
        }
        public T GetItem(string name)
        {
            lock (blockstarsCache)
            {

                return blockstarsCache.Where(x => x.Name == name).First().Value;
            }
        }
        public T[] GetItems(string name)
        {
            lock (blockstarsCache)
            {
                var loc = blockstarsCache.Where(x => x.Name == name).ToArray();
                T[] loc2 = new T[loc.Count()];
                for (int i = 0; i < loc.Count(); i++)
                {
                    loc2[i] = loc[i].Value;
                }
                return loc2;
            }
        }
        private void OnTimerTick(object sender, ElapsedEventArgs e)
        {
            lock (blockstarsCache)
            {
                for (int i = 0; i < blockstarsCache.Count; i++)
                {
                    if (DateTime.Now - blockstarsCache[i].CreationTime >= TimeSpan.FromMinutes(3))
                    {
                        blockstarsCache.RemoveAt(i);
                    }
                }
            }
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Elapsed -= OnTimerTick;
            timer.Dispose();
            blockstarsCache.Clear();
        }
    }
}
