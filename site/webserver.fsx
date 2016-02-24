// --------------------------------------------------------------------------------------
// Start up Suave.io
// --------------------------------------------------------------------------------------

#r "../packages/FAKE/tools/FakeLib.dll"
#r "../packages/Suave/lib/net40/Suave.dll"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "../packages/NodaTime/lib/portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1+XamariniOS1/NodaTime.dll"
#r "../packages/Newtonsoft.Json/lib/portable-net45+wp80+win8+wpa81+dnxcore50/Newtonsoft.Json.dll"
#r "../packages/NodaTime.Serialization.JsonNet/lib/portable-net4+sl5+netcore45+wpa81+wp8+MonoAndroid1+MonoTouch1/NodaTime.Serialization.JsonNet.dll"
#load "Types.fs"
#load "Request.fs"
#load "Process.fs"
#load "TogglReport.fs"

open Fake
open Suave
open Suave.Web
open System.Net
open Suave.Http
open Suave.Filters
open Suave.RequestErrors
open Suave.Successful
open Suave.Writers
open Suave.Operators
open TogglReport
open NodaTime
open NodaTime.Text
open NodaTime.Serialization.JsonNet
open NodaTime.TimeZones
open Newtonsoft.Json

let private datePattern = LocalDatePattern.IsoPattern
let private provider = DateTimeZoneProviders.Tzdb;
let private formatting = 
    let settings = new JsonSerializerSettings()
    settings.DateParseHandling <- DateParseHandling.None
    settings.ConfigureForNodaTime(provider);

let setCORSHeaders =
    setHeader  "Access-Control-Allow-Origin" "*"
    >=> setHeader "Access-Control-Allow-Headers" "content-type"

let private createJsonResponse report =
     setMimeType "application/json" 
     >=> setCORSHeaders 
     >=> OK (JsonConvert.SerializeObject(report, formatting))

let report =
    request (fun r ->
        let workHours = 
            match r.queryParam "workhours" with
            | Choice1Of2 workHours -> int workHours
            | Choice2Of2 _ -> 8
        let date =
            match r.queryParam "date" with
            | Choice1Of2 dateString -> 
                match datePattern.Parse(dateString) with
                | x when x.Success -> Some x.Value
                | _ -> None
            | Choice2Of2 msg -> None
        let token =
            match r.queryParam "token" with
            | Choice1Of2 token -> Some token
            | Choice2Of2 _ -> None

        match (date, token) with
        | (None,_) -> BAD_REQUEST "No Date Specified!"
        | (_, None) -> BAD_REQUEST "No Token Specified!"
        | (Some(dateValue), Some(tokenValue)) -> 
            let report = generateReport workHours tokenValue dateValue

            createJsonResponse report
    )

let allow_cors : WebPart =
    choose [
        OPTIONS >=>
            fun context ->
                context |> (
                    setCORSHeaders
                    >=> OK "CORS approved" )
    ]

let serverConfig = 
    let port = getBuildParamOrDefault "port" "8083" |> Sockets.Port.Parse
    { defaultConfig with bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ] }

let webPart = 
    choose [
        allow_cors
        path "/" >=> report
    ]

startWebServer serverConfig webPart