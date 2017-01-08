namespace Asublog.Plugins
{
    using System;
    using System.Collections.Generic;
    using Core;

    public abstract class Plugin
    {
        public virtual string Name { get; set; }
        public virtual string Version { get; set; }

        public IAsublog App { get; set; }
        public ILogger Log { get; set; }
        public IConfiguration Config { get; set; }

        protected Plugin(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public virtual void Init() { }

        public virtual void Dispose() { }
    }

    public abstract class PostingPlugin : Plugin
    {
        public virtual int PingInterval { get { return 0; } }
        protected PostingPlugin(string name, string version) : base(name, version) { }
        public virtual void Ping() { }
    }

    public abstract class ProcessingPlugin : Plugin
    {
        protected ProcessingPlugin(string name, string version) : base(name, version) { }
        public abstract void Process(Post post);
    }

    public abstract class SavingPlugin : Plugin
    {
        protected SavingPlugin(string name, string version) : base(name, version) { }
        public abstract void Save(Post post);
        public virtual void Flush() { }
        public abstract PostEnumerator GetPosts();
        public abstract void CacheSet(string plugin, string key, string val);
        public abstract string CacheGet(string plugin, string key);
    }

    public abstract class PublishingPlugin : Plugin
    {
        protected PublishingPlugin(string name, string version) : base(name, version) { }
        public abstract void Publish(IEnumerator<Post> posts);
    }

    public abstract class LoggingPlugin : Plugin
    {
        protected LoggingPlugin(string name, string version) : base(name, version) { }
        public abstract void Info(string msg);
        public abstract void Error(string msg, Exception error = null);
        public abstract void Debug(string msg, object obj = null);
    }
}
