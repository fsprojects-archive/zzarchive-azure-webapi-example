# F# Azure WebAPI Example Application

[![Deploy to Azure](http://azuredeploy.net/deploybutton.svg)](https://azuredeploy.net/)

This sample application is an in-memory capabilities-based data store. It is a [CRUD](https://en.wikipedia.org/wiki/Create,_read,_update_and_delete) [RESTful](https://en.wikipedia.org/wiki/Representational_state_transfer) web API built using [Suave](http://suave.io).

## API description

* `PUT /api/create`. The body of the request is placed in the data store, and the API returns a GUID that can be used to read, update, delete, or delegate access to the data.
* `GET /api/read/{guid}`. If the supplied GUID has read access to some data, that data is returned.
* `GET /api/delegate/{access}/{guid}`. Access must be a string that contains some combination of `r` (read), `u` (update), and `d` (delete). If the access requested is the same or a subset of the access on the supplied GUID, a new GUID is returned that has the requested access rights on the supplied GUID.
* `POST /api/update/{guid}`. If the supplied GUID has update access to some data, that data is changed to be the body of the request.
* `DELETE /api/delete/{guid}`. The GUID supplied is deleted. If the supplied GUID has delete access to some data, the entire chain of GUIDs back to the source is deleted.

## Building

The following build files can be used as a template for any [F# Azure App Service](https://azure.microsoft.com/en-us/services/app-service/):

* `build.fsx`. This contains the logic for testing and building your application using [FAKE](http://fsharp.github.io/FAKE/).
* `build.sh` and `build.cmd`. These scripts handle package management with [Paket](https://fsprojects.github.io/Paket/) and invoke FAKE. They do the same thing: the `.sh` is for Linux and OSX, and the `.cmd` is for Windows.
* `.deployment`. This tells Azure App Service how to test and build your application. It invokes the build script.

## Testing

Testing is done with [FsCheck](https://fscheck.github.io/FsCheck/), a property-based testing library derived from [QuickCheck](https://en.wikipedia.org/wiki/QuickCheck). It lets you specify _properties_ about your program, and FsCheck will generate many inputs, attempting to falsify those properties if possible. If FsCheck can falsify a property, it will also give you a reduced test case that causes the problem that you can use for debugging.

## Further Suave Topics

[Suave](http://suave.io) has many useful features for web programming:

* [OAuth integration](https://github.com/OlegZee/Suave.OAuth)
* [Serving static files](https://suave.io/files.html)
* [Music store tutorial](https://www.gitbook.com/book/theimowski/suave-music-store/details)
* [Database integration](https://theimowski.gitbooks.io/suave-music-store/content/en/database.html)
* [E-book tutorial](http://products.tamizhvendan.in/fsharp-applied/)
