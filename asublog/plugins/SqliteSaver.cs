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

    class PostEnumerator : IEnumerator<Post>
    {
        private IEnumerator<DbPost> _posts;
        public PostEnumerator(IEnumerator<DbPost> posts)
        {
            _posts = posts;
        }

        public Post Current { get { return _posts.Current.ToPost(); } }
        object IEnumerator.Current { get { return _posts.Current.ToPost(); } }
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

        public override IEnumerator<Post> GetPosts()
        {
            return new PostEnumerator(_db.Table<DbPost>().OrderByDescending(p => p.Created).GetEnumerator());
        }

        public override void Dispose()
        {
            Log.Info("Closing post database");
            _db.Close();
        }
    }
}
