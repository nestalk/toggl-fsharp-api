namespace TogglReport

module Types =
    open System
    open FSharp.Data
    open NodaTime

    type Workspaces = JsonProvider<""" [
        {
            "id":3134975,
            "name":"John's personal ws",
            "premium":true,
            "admin":true,
            "default_hourly_rate":50,
            "default_currency":"USD",
            "only_admins_may_create_projects":false,
            "only_admins_see_billable_rates":true,
            "rounding":1,
            "rounding_minutes":15,
            "at":"2013-08-28T16:22:21+00:00",
            "logo_url":"my_logo.png"
        }] """>

    type SummaryReport = JsonProvider<""" {
        "total_grand":0,
        "total_billable":0,
        "total_currencies":[{"currency":"","amount":0}],
        "data":[ 
            {
                "id":1,
                "title":{"project": "Project name", "client": "Client name","color":10},
                "time":14400000,
                "total_currencies":[{"currency":"","amount":0}],
                "items":[
                    {
                    "title":{"time_entry": "issue"},
                    "time":14400000,
                    "cur":"",
                    "sum":0,
                    "rate":0
                    }
                ]
            }]
        }
        """>

        
    type TogglRequest = string -> string

    type JobRecord = {
        Client: string
        Activity: string
        Duration: Period
    }