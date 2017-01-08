namespace Asublog.Core
{
    using System.Collections.Generic;

    public interface IConfiguration
    {
        string this[string key] { get; }
    }

    public class Configuration : IConfiguration
    {
        private Dictionary<string, string> _config;

        public Configuration(Dictionary<string, string> config)
        {
            _config = config;
        }

        public string this[string key]
        {
            get
            {
                if(_config == null) return null;
                if(!_config.ContainsKey(key)) return null;
                return _config[key];
            }
        }
    }
}
