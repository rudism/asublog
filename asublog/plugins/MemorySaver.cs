namespace Asublog.Plugins
{
    using System.Collections.Generic;
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

        public override IEnumerator<Post> GetPosts()
        {
            return _posts.GetEnumerator();
        }
    }
}
