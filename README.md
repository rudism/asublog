# as&micro;Blog

## The Antisocial Microblogging Framework

A simple framework for running a self-hosted single-user microblog. To see an example of a microblog generated using as&micro;Blog, check out [status.rudism.com](https://status.rudism.com).

### Posting Methods

- as&micro;Blog will connect to your XMPP server and convert any chat messages you send to it into posts
- as&micro;Blog will monitor your [Pinboard.in](https://pinboard.in) feed for a specific tag and turn those shared links into posts

### Media Extraction & Processing

- Shared dropbox photo urls will embed the photo in the post
- Twitter status urls will quote the linked tweet in your post and embed its image (if it has one)
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

## Usage

1. Rename `config.yml.example` to `config.yml` and edit to suit your needs. See the [example config](https://github.com/rudism/asublog/blob/master/asublog/config.yml.example) to see how it works.

2. Compile and run with mono:

  ```shell
  asublog $> nuget restore
  asublog $> xbuild /t:Release
  asublog $> mono asubuild/bin/Release/asubuild.exe
  ```

3. Start posting to your amazing new single-user microblog!
