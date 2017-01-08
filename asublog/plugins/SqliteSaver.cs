namespace Asublog.Plugins
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using Core;
    using SQLite;

    class DbPost
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }

        public static DbPost FromPost(Post post)
        {
            return new DbPost
            {
                Id = post.Id,
                Created = post.Created,
                Content = post.Content
            };
        }

        public Post ToPost()
        {
            return new Post
            {
                Id = this.Id,
                Created = this.Created,
                Content = this.Content
            };
        }
    }

    class CacheEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Plugin { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    class ConvertedPostEnumerator : IEnumerator<Post>
    {
        private IEnumerator<DbPost> _posts;
        public ConvertedPostEnumerator(IEnumerator<DbPost> posts)
        {
            _posts = posts;
        }

        public Post Current { get { return _posts.Current.ToPost(); } }
        object IEnumerator.Current { get { return Current; } }
        public void Dispose() { _posts.Dispose(); }
        public bool MoveNext() { return _posts.MoveNext(); }
        public void Reset() { _posts.Reset(); }
    }

    public class SqliteSaver : SavingPlugin
    {
        private SQLiteConnection _db;

        public SqliteSaver() : base("sqliteSaver", "0.5") { }

        public override void Init()
        {
            var dbfile = Config["db"];
            var init = !File.Exists(dbfile);
            _db = new SQLiteConnection(dbfile);
            if(init)
            {
                Log.Info(string.Format("Initializing new post database at {0}", dbfile));
                _db.CreateTable<DbPost>();
                _db.CreateTable<CacheEntry>();
            }
            else
            {
                Log.Info(string.Format("Loaded post database at {0}", dbfile));
            }
        }

        public override void Save(Post post)
        {
            _db.Insert(DbPost.FromPost(post));
        }

        public override PostEnumerator GetPosts()
        {
            return App.Wrap(new ConvertedPostEnumerator(_db.Table<DbPost>().OrderByDescending(p => p.Created).GetEnumerator()));
        }

        public override void CacheSet(string plugin, string key, string val)
        {
            var entry = _db.Table<CacheEntry>().Where(e => e.Plugin == plugin && e.Key == key).FirstOrDefault();
            if(entry == null)
            {
                entry = new CacheEntry
                {
                    Plugin = plugin,
                    Key = key,
                };
            }
            entry.Value = val;
            _db.InsertOrReplace(entry);
        }

        public override string CacheGet(string plugin, string key)
        {
            var entry = _db.Table<CacheEntry>().Where(e => e.Plugin == plugin && e.Key == key).FirstOrDefault();
            return entry != null ? entry.Value : null;
        }

        public override void Dispose()
        {
            Log.Info("Closing post database");
            _db.Close();
        }
    }
}
