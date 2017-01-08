namespace Asublog.Plugins
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    public class MemorySaver : SavingPlugin
    {
        private List<Post> _posts;

        public MemorySaver() : base("memorySaver", "1.0")
        {
            _posts = new List<Post>();
        }

        public override void Save(Post post)
        {
            _posts.Add(post);
        }

        public override PostEnumerator GetPosts()
        {
            return App.Wrap(_posts.OrderByDescending(p => p.Created).GetEnumerator());
        }
    }
}
