#r "../packages/FsCheck/lib/net45/FsCheck.dll"

#load "capserver.fsx"

open System
open System.Text
open FsCheck
open Suave
open Suave.Http

let makeReq method urlPath data =
  { HttpRequest.empty with
      method = method
      url = new Uri(sprintf "http://localhost/%s" urlPath)
      rawForm = data
  }

let makeCreate data =
  makeReq Http.PUT "api/create" data

let makeRead guid =
  makeReq Http.GET (sprintf "api/read/%s" (guid.ToString())) Array.empty

let makeDelegate rud guid =
  makeReq Http.GET
    (sprintf "api/delegate/%s/%s" rud (guid.ToString())) Array.empty

let makeUpdate data guid =
  makeReq Http.POST (sprintf "api/update/%s" (guid.ToString())) data

let makeDelete guid =
  makeReq Http.DELETE (sprintf "api/delete/%s" (guid.ToString())) Array.empty

let sendReq req =
  Capserver.app { HttpContext.empty with request = req }
  |> Async.RunSynchronously
  |> Option.get

let getResponse req =
  let context = sendReq req
  match context.response.content with
  | Bytes x -> x
  | _ -> Array.empty

let getGuid req =
  getResponse req
  |> Encoding.UTF8.GetString
  |> Guid.Parse

let getOk req =
  let context = sendReq req
  context.response.status = HttpCode.HTTP_200

let getCode req =
  let context = sendReq req
  context.response.status

type Properties =
  static member ``Let's make a bunch of fake docs and leave them on the server`` data =
    (makeCreate data |> getCode) = HttpCode.HTTP_201

  static member ``Can create and then read`` data =
    let dataResponse =
      makeCreate data
      |> getGuid
      |> makeRead
      |> getResponse
    dataResponse = data

  static member ``Can create and then delete`` data =
    let status =
      makeCreate data
      |> getGuid
      |> makeDelete
      |> getCode
    status = HttpCode.HTTP_204

  static member ``Can create and then update and then delete`` data1 data2 =
    let guid = makeCreate data1 |> getGuid
    let status1 = makeUpdate data2 guid |> getCode
    let dataResponse = makeRead guid |> getResponse
    let status2 = (makeDelete guid |> getCode)
    dataResponse = data2
    && status1 = HttpCode.HTTP_204
    && status2 = HttpCode.HTTP_204

  static member ``Can't read after delete`` data =
    let guid = makeCreate data |> getGuid
    (makeDelete guid |> getCode) = HttpCode.HTTP_204 &&
    (makeRead guid |> getCode) = HttpCode.HTTP_400

  static member ``An invented GUID won't have a doc`` (guid: Guid) =
    (makeRead guid |> getCode) = HttpCode.HTTP_400

  static member ``Can create and then delegate and then read`` data =
    let guid1 = makeCreate data |> getGuid
    let guid2 = makeDelegate "r" guid1 |> getGuid
    let dataResponse = makeRead guid2 |> getResponse
    let status1 = (makeDelete guid1 |> getCode)
    let status2 = (makeDelete guid2 |> getCode)
    dataResponse = data
    && status1 = HttpCode.HTTP_204
    && status2 = HttpCode.HTTP_204

Check.All (Config.QuickThrowOnFailure, typeof<Properties>)
