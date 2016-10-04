#load "capserver.fsx"

open System
open System.Net
open Suave

let port =
  match
    Environment.GetCommandLineArgs()
    |> Array.tryFind (fun arg ->
      arg.StartsWith("port=")
    ) with
  | Some arg -> arg.Split([|'='|]).[1] |> Sockets.Port.Parse
  | None -> 8083us

let serverConfig =
  { defaultConfig with
      bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ] }

startWebServer serverConfig Capserver.app
