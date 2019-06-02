Bing Image Downloader
=================

Simple command line application which will retrive the latest set of Bing images.

Images are downloaded from Bing directly at: http://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=8&mkt={Culture}
E.g. http://www.bing.com/HPImageArchive.aspx?format=xml&idx=20&n=8&mkt=en-GB

Bing Countries
=================

The app will attempt to use every known culture code to get images.

This does result in a lot of duplicate URLs, but the app is also designed to prevent this causing issues!

On Your Mobile
=================

Automate flow http://llamalab.com/automate/community/flows/21377 will update your phone daily

Usage
=================

Setup the 2 app.config values for where to save the images.
Then run the .exe.

You can setup an automated scheduled task in windows to run the exe daily to get more images.

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
