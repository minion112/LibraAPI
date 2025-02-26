using System.Timers;

namespace LibraServer
{
    public struct CacheItem<T> where T : struct
    {
        public DateTime CreationTime { get; set; }
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
        public CacheItem<T> GetItem(string name)
        {
            lock (blockstarsCache)
            {
                return blockstarsCache.Where(x => x.Name == name).First();
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
