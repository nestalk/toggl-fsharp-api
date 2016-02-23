namespace TogglReport

[<AutoOpen>]
module Report =

    open Request
    open Process

    let generateReport workHours token date =
        getReport token date
        |> processReport (int64 workHours)