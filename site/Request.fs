namespace TogglReport

module Request =

    open System
    open System.Text
    open FSharp.Data
    open Types
    open NodaTime
    open NodaTime.Text
    
    let private datePattern = LocalDatePattern.Create("yyyy-MM-dd", Globalization.CultureInfo.CurrentCulture)

    let createAuthHeader token = 
        let apiKey = sprintf "%s:api_token" token
        let keyValue = Convert.ToBase64String( Encoding.ASCII.GetBytes(apiKey))
        sprintf "Basic %s" keyValue

    let togglGetRequest token url = 
        let auth = createAuthHeader token
        Http.RequestString ( url, httpMethod = "GET", headers = [ "Authorization", auth])

    let createRequest token : TogglRequest =
        togglGetRequest token

    let formatDate (date: LocalDate) =
        datePattern.Format(date)

    let getWorkspace (request: TogglRequest) =
        let message = request "https://www.toggl.com/api/v8/workspaces"
        let workspaces = List.ofArray( Workspaces.Parse(message))
        List.head workspaces

    let getSummaryReport (request: TogglRequest) workspaceId (day: LocalDate) = 
        let date = formatDate day
        let requestUrl = sprintf "https://toggl.com/reports/api/v2/summary?workspace_id=%d&since=%s&until=%s&user_agent=togglreport" workspaceId date date
        let reportMessage = request requestUrl
        SummaryReport.Parse(reportMessage)

    let getReport token (date: LocalDate) =
        let request = createRequest token
        let workspace = getWorkspace request
        getSummaryReport request workspace.Id date
