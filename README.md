# Twitter Hashflags Azure Function

[![Build status](https://ci.appveyor.com/api/projects/status/sqp33i4dv7jnoqc7?svg=true)](https://ci.appveyor.com/project/JamieMagee/hashflags-function)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

This Azure Function fetches the active [hashflags](http://hashfla.gs/) from Twitter, and stores them in a JSON object in an Azure Storage Blob. You can find the list of current hashflags [here](https://hashflags.blob.core.windows.net/json/activeHashflags)

## What is a Hashflag?

A hashflag, sometimes called Twitter emoji, is a small image that appears after a *#hashtag* for special events. They are not regular emoji, and you can only use them on the Twitter website, or the official Twitter apps.

![Hashflags from Eurovision 2015](https://i.imgur.com/f2tdQc3.png)
Hashflags from Eurovision 2015

## Why create an Azure Function?

Currently Twitter doesn't provide an official API for hashflags, and there is no official list of currently active hashflags. [@hashflaglist](https://twitter.com/hashflaglist) tracks hashflags, but it's easy to miss one – especially as many of them are temporary.

The aim of this project is to allow people to use hashflags outside of Twitter, and use them in their own applications. In same the way that you can miss context when an emoji doesn't display correctly, hashflags are integral to talking about Twitter trends outside of the microcosm of Twitter. In the Eurovision example above, it's very hard to place the three letter hashtag alone without the distinctive Eurovision hashflag.