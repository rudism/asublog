namespace Asublog.Plugins
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    public class MemorySaver : SavingPlugin
    {
        private Dictionary<string, Dictionary<string, string>> _cache;
        private List<Post> _posts;

        public MemorySaver() : base("memorySaver", "1.0")
        {
            _cache = new Dictionary<string, Dictionary<string, string>>();
            _posts = new List<Post>();
        }

        public override void Save(Post post)
        {
            _posts.Add(post);
        }

        public override int PostCount
        {
            get { return _posts.Count; }
        }

        public override PostEnumerator GetPosts()
        {
            return App.Wrap(((IEnumerable<Post>) _posts.OrderByDescending(p => p.Created).ToArray()).GetEnumerator());
        }

        public override void CacheSet(string plugin, string key, string val)
        {
            if(!_cache.ContainsKey(plugin))
            {
                _cache.Add(plugin, new Dictionary<string, string>());
            }
            if(!_cache[plugin].ContainsKey(key))
            {
                _cache[plugin].Add(key, val);
            }
            else
            {
                _cache[plugin][key] = val;
            }
        }

        public override string CacheGet(string plugin, string key)
        {
            if(!_cache.ContainsKey(plugin)) return null;
            if(!_cache[plugin].ContainsKey(key)) return null;
            return _cache[plugin][key];
        }
    }
}
