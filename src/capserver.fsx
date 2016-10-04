#r "../packages/Suave/lib/net40/Suave.dll"

open System
open System.Collections.Concurrent
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors

type Permission = {
  Read: bool
  Update: bool
  Delete: bool
}

type Document =
  | Guid of Guid
  | Data of byte []

type Capability = Permission * Document

let db = ConcurrentDictionary<Guid, Capability>()

let emptyPermission =
  { Read = false
    Update = false
    Delete = false }

let fullPermission =
  { Read = true
    Update = true
    Delete = true }

let getPermission (rud: string) =
  { Read = rud.Contains "r"
    Update = rud.Contains "u"
    Delete = rud.Contains "d" }

let toGuid s =
  let mutable guid = Guid.Empty
  Guid.TryParse(s, &guid) |> ignore
  guid

let getCap guid =
  match db.TryGetValue(guid) with
  | (true, cap) -> cap
  | (false, _) -> emptyPermission, Guid Guid.Empty

let getData (req: HttpRequest) =
  req.rawForm

let create data =
  let guid = Guid.NewGuid()
  db.TryAdd(guid, (fullPermission, Data data)) |> ignore
  CREATED (guid.ToString())

let makeCap newPermission guid =
  let permission = fst (getCap guid)
  if (newPermission <> emptyPermission) && (newPermission <= permission) then
    let newGuid = Guid.NewGuid()
    db.TryAdd(newGuid, (newPermission, Guid guid)) |> ignore
    OK (newGuid.ToString())
  else
    BAD_REQUEST "Not allowed to delegate those permissions"

let rec read guid =
  let (permission, doc) = getCap guid
  if permission.Read then
    match doc with
    | Guid source -> read source
    | Data data -> ok data
  else
    BAD_REQUEST "Not allowed to read"

let rec update guid data =
  let (permission, doc) = getCap guid
  if permission.Update then
    match doc with
    | Guid source -> update source data
    | Data _ ->
      let value = permission, Data data
      db.AddOrUpdate(guid, value, fun _ _ -> value) |> ignore
      NO_CONTENT
  else
    BAD_REQUEST "Not allowed to update"

let rec delete guid =
  let (permission, doc) = getCap guid
  let mutable prev = emptyPermission, Guid Guid.Empty
  db.TryRemove(guid, &prev) |> ignore
  if permission.Delete then
    match doc with
    | Guid source -> delete source
    | _ -> NO_CONTENT
  else
    NO_CONTENT

let intro =
  """
<html>
<head><title>Capability-based Data Store</title></head>
<body>
This sample application is an in-memory capabilities-based data store. It is a <a href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</a> <a href="https://en.wikipedia.org/wiki/Representational_state_transfer">RESTful</a> web API built using <a href="http://suave.io">Suave</a>.

<h2>API description</h2>

<ul>
<li><code>PUT /api/create</code>. The body of the request is placed in the data store, and the API returns a GUID that can be used to read, update, delete, or delegate access to the data.
<li><code>GET /api/read{guid}</code>. If the supplied GUID has read access to some data, that data is returned.
<li><code>GET /delegate/{access}/{guid}</code>. Access must be a string that contains some combination of <code>r</code> (read), <code>u</code> (update), and <code>d</code> (delete). If the access requested is the same or a subset of the access on the supplied GUID, a new GUID is returned that has the requested access rights on the supplied GUID.
<li><code>POST /api/update/{guid}</code>. If the supplied GUID has update access to some data, that data is changed to be the body of the request.
<li><code>DELETE /api/delete/{guid}</code>. The GUID supplied is deleted. If the supplied GUID has delete access to some data, the entire chain of GUIDs back to the source is deleted.
</ul>
</body>
"""

let app =
  choose
    [ GET >=> path "/" >=> OK intro
      PUT >=> path "/api/create" >=> request (getData >> create)
      GET >=> pathScan "/api/read/%s" (toGuid >> read)
      GET >=> pathScan "/api/delegate/%s/%s" (fun (rud, id) ->
        toGuid id
        |> makeCap (getPermission rud)
      )
      POST >=> pathScan "/api/update/%s" (fun id ->
        let guid = toGuid id
        request (getData >> update guid)
      )
      DELETE >=> pathScan "/api/delete/%s" (toGuid >> delete)
    ]
