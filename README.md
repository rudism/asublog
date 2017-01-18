# as&micro;Blog

## The Antisocial Microblogging Framework

A simple framework for running a self-hosted single-user microblog. To see an example of a microblog generated using as&micro;Blog, check out [status.rudism.com](https://status.rudism.com).

### Posting Methods

- as&micro;Blog will connect to your XMPP server and convert any chat messages you send to it into posts
- as&micro;Blog will monitor your [Pinboard.in](https://pinboard.in) feed for a specific tag and turn those shared links into posts
- send new posts to a local TCP port on the asu&micro;Blog server
  - can be used for IPC between as&micro;Blog and other processes (web servers, scripts, etc.)

### Media Extraction & Processing

- OpenGraph image metadata retrieved from linked urls, resized if necessary, and embedded in posts
- Twitter status urls will quote the linked tweet in your post
- Convert urls into links
- Shorten links using a [lilurl](http://lilurl.sourceforge.net) domain
- Auto link `#hashtags`
- Auto link `@Usernames` to custom urls or Twitter profiles

### Publishing Methods

- Generate static site from [Handlebars](http://handlebarsjs.com) templates and upload to an S3 bucket
- Optionally create invalidations against a CloudFront distribution

## Extensibility

Written with extensibility in mind. You can write and run as many logging, posting, and publishing plugins as you can dream up to create posts from any number of sources and save them to any number of formats and destinations.

The only real limitation is you can just use one data saving plugin at a time. Currenly available are an ephemeral in-memory store or a SQLite database.

### Posting Plugins

Posting plugins can either spin up their own thread to capture and create new posts at will (see [`XmppPoster`](https://github.com/rudism/asublog/blob/master/asublog/plugins/XmppPoster.cs) for an example), or can be pinged at a set interval to check for and create new posts (see [`PinboardPoster`](https://github.com/rudism/asublog/blob/master/asublog/plugins/PinboardPoster.cs) for an example).

### Saving Plugins

Saving plugins are responsible for persisting posts as well as acting as a key-value cache for other plugins. See [`MemorySaver`](https://github.com/rudism/asublog/blob/master/asublog/plugins/MemorySaver.cs) for a simple example which does not persist data between application runs.

### Publishing Plugins

Whenever a new post is received, publishing plugins are triggered with all of the blog's posts. See [`ConsolePublisher`](https://github.com/rudism/asublog/blob/master/asublog/plugins/ConsolePublisher.cs) for a simple example that just publishes new posts to the console.

Some other possibilities would be a publisher that mirrors new posts to Twitter or automatically shares them to Facebook. These have not been written yet. Pull requests are welcome!

### Processing Plugins

Processing plugins are run against every new post prior to publishing them. Existing plugins do things like convert urls into links (see [`HtmlizeProcessor`](https://github.com/rudism/asublog/blob/master/asublog/plugins/HtmlizeProcessor.cs) for this example), extract image urls to generate embedded media, and pass links through a url shortener.

### Logging Plugins

Currently the only logging plugin is [`ConsoleLogger`](https://github.com/rudism/asublog/blob/master/asublog/plugins/ConsoleLogger.cs) which dumps log output to the console, but others could be written to suit your needs.

## Usage

1. Rename `config.yml.example` to `config.yml` and edit to suit your needs. See the [example config](https://github.com/rudism/asublog/blob/master/asublog/config.yml.example) to see how it works. You must specify one saver plugin, but can specify as many or as few of the other plugins as suit your needs.

2. Compile and run with mono:

  ```shell
  asublog $> nuget restore
  asublog $> xbuild /t:Release
  asublog $> mono asubuild/bin/Release/asubuild.exe
  ```

3. Start posting to your amazing new single-user microblog!
