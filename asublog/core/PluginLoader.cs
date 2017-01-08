namespace Asublog.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using YamlDotNet.RepresentationModel;
    using Plugins;

    class PluginLoader
    {
        private List<Plugin> _plugins;

        public PluginLoader(YamlMappingNode config, ILogger log, IAsublog app)
        {
            _plugins = new List<Plugin>();
            var plugins = (YamlSequenceNode) config.Children[new YamlScalarNode("plugins")];
            foreach(YamlScalarNode pluginName in plugins)
            {
                var pluginType = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Plugin))).Single(p => p.Name.Equals(pluginName.Value, StringComparison.OrdinalIgnoreCase));

                if(pluginType == null)
                {
                    throw new Exception(string.Format("Plugin type {0} could not be found.", pluginName.Value));
                }

                Dictionary<string, string> plugDict = null;
                var plugConfigKey = new YamlScalarNode(string.Format("{0}Config", pluginName.Value));
                if(config.Children.ContainsKey(plugConfigKey))
                {
                    var plugConfig = (YamlMappingNode) config.Children[plugConfigKey];

                    plugDict = new Dictionary<string, string>();
                    foreach(YamlScalarNode key in plugConfig.Children.Keys)
                    {
                        plugDict.Add(key.Value, ((YamlScalarNode) plugConfig.Children[key]).Value);
                    }
                    log.Debug(string.Format("Got config for plugin {0}", pluginName.Value), plugDict);
                }
                else
                {
                    log.Debug(string.Format("No config found for plugin {0}", pluginName.Value));
                }

                try
                {
                    var plugin = Activator.CreateInstance(pluginType) as Plugin;

                    plugin.App = app;
                    plugin.Log = log;
                    plugin.Config = new Configuration(plugDict);

                    plugin.Init();

                    _plugins.Add(plugin);
                }
                catch(Exception ex)
                {
                    log.Error(string.Format("Error while initializing plugin {0}", pluginName.Value), ex);
                }
            }
        }

        public T[] GetPlugins<T>() where T : Plugin
        {
            return _plugins.Where(p => p.GetType().IsSubclassOf(typeof(T))).Cast<T>().ToArray();
        }
    }
}
