Bing Image Downloader
=================

[![AppVeyor branch](https://img.shields.io/appveyor/ci/blythmeister/bingimagedownload)](https://ci.appveyor.com/project/BlythMeister/BingImageDownload)
[![Nuget](https://img.shields.io/nuget/v/bingimagedownload)](https://www.nuget.org/packages/BingImageDownload/)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/BlythMeister/BingImageDownload)](https://github.com/BlythMeister/BingImageDownload/releases/latest)
[![GitHub issues](https://img.shields.io/github/issues-raw/blythmeister/bingimagedownload)](https://github.com/BlythMeister/BingImageDownload/issues)

Simple command line application which will retrive the latest set of Bing images.

Images are downloaded from Bing directly at: https://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=8&mkt={Culture}
E.g. https://www.bing.com/HPImageArchive.aspx?format=xml&idx=20&n=8&mkt=en-GB

Bing Countries
=================

The app will attempt to use every known culture code to get images.

This does result in a lot of duplicate URLs, but the app is also designed to prevent this causing issues!

On Your Mobile
=================

Automate flow http://llamalab.com/automate/community/flows/21377 will update your phone daily

Usage
=================

```
Usage: BingImageDownload [options]

Options:
  -p|--path <PATH>         The path to where images should be saved (Default: Home Path)
  -a|--archive <VALUE>     The number of months to archive after (Default: 1)
  -d|--delete <VALUE>      The number of months to delete after (Default: never)
  -r|--resolution <VALUE>  The resolution for images (HD,FHD,UHD) (Default: FHD)
  -?|-h|--help             Show help information.
```

You can setup an automated scheduled task in windows to run the dotnet tool daily to get more images.

Setup you desktop background on the save folder & enjoy auto changing Bing images on your desktop!

Licence
=================

This application is covered by the GPLv2 licence agreement.

Note
=================

This application is not affiliated with Bing directly.

I am purely making use of the web endpoint they provide, and accessing the images on the URLs contained in their endpoint.

3rd Party Components
=================

AForge Imaging: http://www.aforgenet.com/framework/
(Used for Histogram creation for image duplicate checking)
