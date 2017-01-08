namespace Asublog.Core
{
    using System;

    public class Post
    {
        public Guid Id { get; private set; }
        public DateTime Created { get; private set; }
        public string Content { get; set; }

        public Post()
        {
            Id = Guid.NewGuid();
            Created = DateTime.UtcNow;
        }
    }
}
