*Note: This project is still in early stages of development and not yet functional in any way.*

# as&micro;Blog

## The Anti-Social Microblogging Framework

A simple framework for running a self-hosted single-user microblog.

### Posting Methods

- as&micro;Blog will connect to your XMPP server and convert any chat messages you send to it into posts
- as&micro;Blog will monitor your [Pinboard.in](https://pinboard.in) feed for a specific tag and turn those shared links into posts
- going to think up a good way to do image posts as well

### Publishing Methods

- Handlebars templates uploaded to an S3 bucket (index, post details, rss feed)

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
