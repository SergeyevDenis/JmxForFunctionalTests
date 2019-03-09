module JtlParser.Jmeter
open System.IO
open System.Diagnostics
open System.Collections.Generic
open System

let startProcess (processExePath:string) args= 
    let consoleOutput (sender:obj) data =
        let processName = Path.GetFileNameWithoutExtension (processExePath)
        printfn "%s(%i): %s" processName (sender :?> Process).Id data
    
    let startParams = 
        ProcessStartInfo(
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            FileName = processExePath,
            Arguments = args
        )
    let outputHandler f (sender:obj) (args:DataReceivedEventArgs) = f sender args.Data

    let p = new Process(StartInfo = startParams)
    p.OutputDataReceived.AddHandler(DataReceivedEventHandler (outputHandler consoleOutput))
    p.ErrorDataReceived.AddHandler(DataReceivedEventHandler (outputHandler consoleOutput))
    let started = 
        try
            p.Start()
        with | ex ->
            ex.Data.Add("filename", processExePath)
            reraise()
    if not started then
        failwithf "Failed to start process %s" processExePath
    printfn "Started %s with pid %i%s" (Path.GetFileNameWithoutExtension (processExePath)) p.Id Environment.NewLine
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()
    p.WaitForExit()
    printfn "Finished %s pid %i %s" processExePath p.Id Environment.NewLine
    
let getJtlFIleName (jmxPath:string) =
    Path.Combine (Path.GetDirectoryName(jmxPath),(Path.GetFileNameWithoutExtension (jmxPath))) + ".jtl"

let runJmx jmeterPath jmxPath = 
    let jtlPath = getJtlFIleName jmxPath
    if File.Exists(jtlPath) then File.Delete(jtlPath)
    let args = sprintf """-n -t "%s" -l "%s" """ jmxPath jtlPath
    startProcess (Path.Combine (jmeterPath, "jmeter.bat")) args |> ignore

let runAllJmxInDirectory jmeterPath (directoryPath:string) =
    let jmxFiles = Directory.GetFiles (directoryPath, "*.jmx")
    jmxFiles |> Array.Parallel.map (fun jmx -> runJmx jmeterPath jmx) |> ignore
    jmxFiles |> Array.map (getJtlFIleName)