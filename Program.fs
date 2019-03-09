module JtlParser.Main

open System

[<EntryPoint>]
let main argv =
    if argv.Length <> 2 then
        printfn "В качестве аргументов передать 2 значения: Путь до jmeter.bat и путь до папки с файлами .jmx"
        1
    else    
        let jmeterPath = argv.[0]
        let jmxDirectory = argv.[1]
        let jtls = Jmeter.runAllJmxInDirectory jmeterPath jmxDirectory
        async {
            return! Report.generate jtls
        } |> Async.RunSynchronously
        0