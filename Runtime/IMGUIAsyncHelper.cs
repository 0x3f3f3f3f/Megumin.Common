using System.Collections.Generic;
using System.Threading.Tasks;

namespace Megumin
{
    public abstract class IMGUIAsyncHelper<T>
    {
        public Dictionary<string, T> CacheResult { get; } = new();

        public bool TryGetResultOnGUI(string key, bool click, out T result, object options = null)
        {
            if (click)
            {
                OpenMenu(key, options);
            }

            if (CacheResult.TryGetValue(key, out result))
            {
                CacheResult.Remove(key);
                return true;
            }

            result = default;
            return false;
        }

        public async void OpenMenu(string key, object options = null)
        {
            var str = await GetResult(options);
            CacheResult[key] = str;
        }

        public abstract Task<T> GetResult(object options = null);
    }
}
