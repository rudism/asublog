namespace Asublog.Core
{
    using System;

    public class Attachment
    {
        public string Type { get; set; }
        public string Content { get; set; }
    }

    public class Post
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }

        public bool Processed { get; set; }

        public Attachment[] Attachments { get; set; }

        public Post()
        {
            Id = Guid.NewGuid();
            Created = DateTime.UtcNow;
        }
    }
}
