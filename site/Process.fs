namespace TogglReport

module Process =
    
    open System
    open Types
    open NodaTime
    
    let roundTime (ms:int) =
        let time = TimeSpan.FromMilliseconds(float ms)
        let hours = time.Hours

        let builder = new PeriodBuilder()
        builder.Hours <- int64 hours

        match time.Minutes with
        | x when x <= 15 -> builder.Minutes <- int64 15
        | x when x <= 30 -> builder.Minutes <- int64 30
        | x when x <= 45 -> builder.Minutes <- int64 45
        | x when x > 45 -> builder.Hours <- builder.Hours + int64 1
        | _ -> ()

        builder.Build()

    let toJobRecord (item: SummaryReport.Datum) =
        let time = roundTime item.Time
        { Client = item.Title.Client; Activity = item.Title.Project; Duration = time }

    let createJobRecords (report: SummaryReport.Root) =
        report.Data
        |> Array.sortBy (fun x -> x.Time)
        |> Array.map toJobRecord
        |> Array.toList

    let calculateOverrun workHours (records: JobRecord list) =
        let totalDuration = List.fold (fun (duration:Period) time -> duration + time.Duration) (Period.Zero) records
        let workPeriod = Period.FromHours(workHours)
        let comparer = Period.CreateComparer(new LocalDateTime())
        match totalDuration.Normalize() with
        | x when comparer.Compare(x, workPeriod) > 0 -> Some(x - workPeriod)
        | _ -> None

    let subtractPeriod (period:Period) (overrun:Period) =
        if period.Minutes < overrun.Minutes then
            let builder = new PeriodBuilder()
            builder.Hours <- period.Hours - overrun.Hours - int64 1
            builder.Minutes <- period.Minutes - overrun.Minutes + int64 60
            builder.Build()
        else
            period - overrun

    let rec applyOverrun (records: JobRecord list) (overrun: Period) =
        match records with
        | [] -> []
        | [record] -> 
            let compensated = subtractPeriod record.Duration overrun
            [{ Client = record.Client; Activity = record.Activity; Duration = compensated }]
        | x::xs -> x :: applyOverrun xs overrun

    let processReport workHours (report: SummaryReport.Root) =
        let records = createJobRecords report
        let overrun = calculateOverrun workHours records
        let report = match overrun with
                        | Some(over) -> applyOverrun records over
                        | None -> records 
        report |> List.toArray
        