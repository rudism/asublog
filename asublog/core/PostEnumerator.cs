namespace Asublog.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Plugins;

    public class PostEnumerator : IEnumerator<Post>
    {
        public ILogger Log { get; set; }
        public IEnumerator<Post> Posts { get; set; }
        public IEnumerable<ProcessingPlugin> ProcessingPlugins { get; set; }

        public Post Current
        {
            get
            {
                var current = Posts.Current;
                if(!current.Processed)
                {
                    Log.Debug(string.Format("Processing post {0}", current.Id));
                    foreach(var plugin in ProcessingPlugins)
                    {
                        try
                        {
                            plugin.Process(current);
                        }
                        catch(Exception ex)
                        {
                            Log.Error(string.Format("Error while processing with plugin {0}", plugin.Name), ex);
                        }
                    }
                    current.Processed = true;
                }
                return current;
            }
        }

        object IEnumerator.Current { get { return Current; } }
        public void Dispose() { Posts.Dispose(); }
        public bool MoveNext() { return Posts.MoveNext(); }
        public void Reset() { Posts.Reset(); }
    }
}
