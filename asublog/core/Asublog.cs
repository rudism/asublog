namespace Asublog.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using YamlDotNet.RepresentationModel;
    using Plugins;

    public interface IAsublog
    {
        ILogger Log { get; set; }
        void ReceivePost(Post post);
        void Dispose();
    }

    class Asublog : IAsublog
    {
        private LoggingPlugin[] _loggingPlugins;
        private PostingPlugin[] _postingPlugins;
        private PublishingPlugin[] _publishingPlugins;

        private SavingPlugin _savingPlugin;

        private Dictionary<Guid, Timer> _timers;

        public ILogger Log { get; set; }

        internal void Run()
        {
            // load configuration
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yml");
            var yaml = new YamlStream();
            yaml.Load(new StreamReader(path));
            var config = yaml.Documents[0].RootNode as YamlMappingNode;

            // use console logger temporarily while loading plugins
            Log = new Logger
            {
                Loggers = new[] { new ConsoleLogger() }
            };

            // load plugins
            var loader = new PluginLoader(config, Log, this);
            _loggingPlugins = loader.GetPlugins<LoggingPlugin>();
            _postingPlugins = loader.GetPlugins<PostingPlugin>();
            _publishingPlugins = loader.GetPlugins<PublishingPlugin>();

            var saving = loader.GetPlugins<SavingPlugin>();
            if(saving.Length != 1)
            {
                throw new Exception("Must specify exactly one SavingPlugin");
            }
            _savingPlugin = saving.Single();

            // replace temporary console logger with actual logging plugins
            Log.Loggers = _loggingPlugins;

            // set up pings for posting plugins
            _timers = new Dictionary<Guid, Timer>();
            foreach(var plugin in _postingPlugins)
            {
                var id = Guid.NewGuid();
                if(plugin.PingInterval > 0)
                {
                    var timer = new Timer((s) =>
                    {
                        try
                        {
                            Log.Info(string.Format("Pinging plugin {0}", plugin.Name));
                            plugin.Ping();
                        }
                        catch(Exception ex)
                        {
                            Log.Error(string.Format("Error while pinging plugin {0}", plugin.Name), ex);
                        }
                        _timers[id].Change(plugin.PingInterval * 1000, Timeout.Infinite);
                    }, null, plugin.PingInterval * 1000, Timeout.Infinite);
                    _timers.Add(id, timer);
                }
            }
        }

        public void ReceivePost(Post post)
        {
            ReceivePosts(new[] {post});
        }

        public void ReceivePosts(IEnumerable<Post> posts)
        {
            foreach(var post in posts)
            {
                try
                {
                    _savingPlugin.Save(post);
                }
                catch(Exception ex)
                {
                    Log.Error("Error while saving post in plugin {0}", ex);
                }
            }
            try
            {
                _savingPlugin.Flush();
            }
            catch(Exception ex)
            {
                Log.Error("Error while flushing plugin {0}", ex);
            }
            foreach(var plugin in _publishingPlugins)
            {
                plugin.Publish(_savingPlugin.GetPosts());
            }
        }

        public void Dispose()
        {
            foreach(Plugin plugin in _postingPlugins.Cast<Plugin>().Union(new[] {_savingPlugin}).Union(_loggingPlugins).Union(_publishingPlugins))
            {
                plugin.Dispose();
            }
        }
    }
}