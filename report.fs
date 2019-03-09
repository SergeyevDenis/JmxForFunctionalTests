module JtlParser.Report

open FSharp.Control
open System.IO
open System

let getFileContent (path:string) = 
    async {
        use reader = new StreamReader (path)
        return! reader.ReadToEndAsync() |> Async.AwaitTask
    } 

let saveFile (content:string) path =
    async {
        if File.Exists (path) then File.Delete(path)

        use writer = new StreamWriter(File.OpenWrite(path))
        return! writer.WriteAsync(content) |> Async.AwaitTask
    }
type RequestSectionData = {Label:string; ResponceCode: int; ResponceMessage:string; FailureMessage:string; Success:bool; Url:string}
type Report(jtlFiles:string[]) =
    let jtlFileSectionTemplatePath = ".\\template\\JtlFileSection.html"
    let jtlFileRequestSectionTemplatePath = ".\\template\\JtlFileRequestSection.html"
    let htmlTemplatePath = ".\\template\\output.html"

    let _totalTestsCount = jtlFiles.Length
    let _jtlFiles:string[] = jtlFiles

    let setTemplateVariable (varNme:string) varValue (template:string)= 
        template.Replace("{{"+varNme+"}}", varValue.ToString())

    let jtlRequestSectionData (line:string) =
        let extracSectionParts (arr:string[]) =
            let respCode = Int32.Parse arr.[3]
            let success = bool.Parse arr.[7]
            { Label = arr.[2]; ResponceCode = respCode; ResponceMessage = arr.[4]; FailureMessage = arr.[8]; Success = success; Url = arr.[13]}

        line.Split ('\t') |> extracSectionParts

    let getJtlContent (jtlPath:string) = 
        async {
            let! lines = File.ReadAllLinesAsync (jtlPath) |> Async.AwaitTask
            return Array.tail lines |> Array.Parallel.map jtlRequestSectionData
        }

    member
        this.GenerateOutput (outputPath:string) =
            async {
                let! jtlFileSectionTemplate = getFileContent jtlFileSectionTemplatePath
                let! jtlRequestSectionTemplate = getFileContent jtlFileRequestSectionTemplatePath

                let getDataForTemplate (fileName:string) (sections:RequestSectionData[]) = seq {
                    if Array.exists (fun (sect:RequestSectionData) -> (not sect.Success)) sections then
                        yield jtlFileSectionTemplate
                                |> setTemplateVariable "fileName" fileName 
                                |> setTemplateVariable "testStatus" "Error"
                                |> setTemplateVariable "style" "class=\"error\""
                        for sect in sections do
                            let styleVal = if sect.Success then "class=\"success\"" else "class=\"error\""
                            yield jtlRequestSectionTemplate
                                |> setTemplateVariable "request" sect.Url  
                                |> setTemplateVariable "responseCode" sect.ResponceCode 
                                |> setTemplateVariable "testStatus" sect.ResponceMessage
                                |> setTemplateVariable "label" sect.Label
                                |> setTemplateVariable "failureMessage" sect.FailureMessage
                                |> setTemplateVariable "style" styleVal
                    else
                        yield jtlFileSectionTemplate 
                                |> setTemplateVariable "fileName" fileName 
                                |> setTemplateVariable "testStatus" "Ok"
                                |> setTemplateVariable "style" "class=\"success\""
                }

                let fileProcess file = 
                    async {
                        let! jtlContent = getJtlContent file
                        return getDataForTemplate file jtlContent |> Seq.reduce (+)
                    } |>Async.RunSynchronously

                let dataStr = _jtlFiles |> Array.Parallel.map fileProcess |> Array.reduce ( + )

                let! template = getFileContent htmlTemplatePath
                let saveFileTo = saveFile (setTemplateVariable "testsCompleted" _totalTestsCount template |> setTemplateVariable "data" dataStr)
                return! saveFileTo outputPath 
            } 

let generate (jtlFiles:string[]) =
    let outputfile = ".\\result.html"
        
    let report = Report(jtlFiles)
    report.GenerateOutput outputfile