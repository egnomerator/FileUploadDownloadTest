# FileUploadDownloadTest
POC for a resource-conscious, large-file library web API

The focus so far has been on implementing a simple API that facilitates file upload/download with:

- asynchronous request handling to facilitate many concurrent requests
- file upload/download by streaming in chunks to use RAM more efficiently and to handle very large files
- Kestrel max current requests set and rate limiting middleware for some protection against high load

---

in lieu of a decent README file, this is a stream of stream-of-consciousness/brainstorm for me to come back to ...

---

## top questions

- best way to **persist** files? in DB? on FS? is there a generally accept preferred way to store these for optimal performance?
    - performance: read/write speed; disk space;
    - is there a nice library or accepted approach to storing compressed media files?
- is there a nice library or accepted approach to working with **files in memory**?
    - compressed?
- file chunking
- **transferring** (sending/receiving) media **files over network**
    - is there a nice library or accepted approach to transferring files over a network in a performance-centric manner
    - performance: fast transfer, compressed, anything else?
- middleware DI
    - scoping for reuse over multiple requests where possible
- high-level file upload/download API strategy

other questions

- HTTPS
    - if not too much trouble, find a way to create a self-signed cert at build time if not already exists
- authentication/authorization
    - constraints specifically this isn't needed, but just give some thought at least
    - could at least create an identity interface and use DI to pass in a AllowAnonymous impl
    - could mayyybe attempt a simplistic RBAC interface just to have a way forward in implementing this safely
- think in terms of media types? file types?
- is supporting music files different from supporting video files?
- cancel mid upload/download
- api versioning
- disk space
    - this could obviously take a ton of storage space
    - what's the storage strategy?
    - how to get fast storage
    - how to support a large amount of storage
- response caching (lots of data)
- request throttling
    - kestrel can handle many 100Ks of concurrent requests potentially
    - but with large files, performance will degrade eventually
- performance
    - memory...LOH and Gen 2 GC
    - or worse--OOM
- metadata
    - how to get metadata from file
    - file formats
- failure handling
    - write fails (max retries approach? and if one fails, cleanup/delete the one that succeeded?)
        - file write fails
        - database write fails
    - read fails (fail fast? no retries?)
        - of either one fails, immediately stop
- saving to long paths
    - should just have control over this and prevent
    - validate for max file name length?
- security/validation
    - virus scanning
    - disable execute permissions
    - max file size
    - user-provided file name (untrusted: store and display HTML encoded name only (path-removed), generate name for storage)
- media file processing library?
    - ffmpeg is in C and can't find a .NET version
        - there are a few .net libs that seem to try wrapping it to varying degrees
- ID3
    - [TagLibSharp (v2.2.0)](https://www.nuget.org/packages/TagLibSharp)
    - this seems pretty popular, looks like it will do what i need, [supports .NET Standard 2](https://github.com/mono/taglib-sharp/issues/60#issuecomment-455946726)
- how to adequately test an APIs ability to handle high volumes of concurrent requests
    - e.g. smartbear loadui pro
    - e.g. Apache Bench recommended by MS team [here](https://github.com/dotnet/aspnetcore/issues/3009#issuecomment-377570704)
    - ... and when streaming lots of data?
    - testing over the network? not just locally

assumptions

- server can enforce that humans don't touch the files on disk


features

- future
    - support video files (is this different? mp4?)
    - what file types to support
    - generate revenue for the musician
        - ability to build a fan base
        - incentivize fans with early access to new songs, etc.
    - play musing while streaming?
    - licensing concerns
- now
    - upload/download music files
    - what supported file types?
    - batch upload/download
    - cancellation



entities

- file
- mediaType
    - music
    - video
- userType
    - artist
    - consumer
- user
- relationships
    - user is-a-fan-of user
    - user is family-related with user
- family
    - (family plan--all family members have access to media libraries)
