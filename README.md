*Note: This project is still in early stages of development and not yet functional in any way.*

# AS&micro;Blog

## The Anti-Social Microblogging Framework

A simple framework for running a self-hosted single-user microblog.

### Posting Methods

- Send a chat message to an XMPP bot
- Share a link on [Pinboard.in](https://pinboard.in) with a specific tag

### Publishing Methods

- HTML template uploaded to S3

## Extensibility

Written with extensibility in mind. Write your own plugins to generate and publish posts from and to any source imaginable.

## Usage

Create your configuration:

```shell
asublog $> cp asublog/config.yml.example asublog/config.yml
asublog $> vim asublog/config.yml
```

```yaml
plugins:
  - consoleLogger
  - xmppPoster
  - pinboardPoster
  - s3Saver

xmppPosterConfig:
  host: my-xmpp-server.com
  jid: mybot@my-xmpp-server.com
  password: my-super-secret-password
  authorized: me@my-xmpp-server.com

pinboardPosterConfig:
  username: myusername
  password: mypassword
  tag: microblog
  interval: 300

s3SaverConfig:
  bucket: mysweetmicroblog.com
  template: /path/to/my/template.html
  postsPerPage: 30
```

Compile and run with mono:

```shell
asublog $> nuget restore
asublog $> xbuild /t:Release
asublog $> mono asubuild/bin/Release/asubuild.exe
```
