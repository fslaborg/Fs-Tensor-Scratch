#if FAKE
#r "paket: groupref Build //" 
#endif
#load "./versionnumbering.fsx"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO

let ReleaseNotesLocation = System.IO.Path.Combine [|__SOURCE_DIRECTORY__;"./ReleaseNotes.txt"|]

type PublicDirs () =
    static member Packagedirectory = System.IO.Path.Combine [|__SOURCE_DIRECTORY__;"./nuget_package"|] 
    static member Packagerepository = "" 

module BuildDomain =
    type Projectname =
        |FsTensor
    type Projectfile = Projectfile of string
    type Directory = Directory of string
    type Version = Version of string
    type VersionDescription = VersionDescription of string
    type Repository = Repository of string
    type Assemblyfile = Assemblyfile of string
    type Publishfunction = Directory -> Repository -> unit
    type ReleaseNotesAssemblyFile =
        {
            version:Version   
            assemblyfile:Assemblyfile
            attributes:AssemblyInfoFile.Attribute list
        }
    type BuildStep =
        |Clean of Directory list
        |Versioning of ReleaseNotesAssemblyFile
        |PrePackage of Projectfile*Directory
        |Package  of Directory*Version
        |Publish of Publishfunction*Projectname*Version*VersionDescription  

module TargetBuilders =
    open BuildDomain
    let list_of_targets ((project,stappen):Projectname*BuildStep list) =
        let projectname = sprintf "%A" project
        stappen
        |> List.map
            (fun stap ->
                match stap with
                |Clean _ -> stap,"Clean"+projectname |> List.singleton
                |Versioning _ -> stap,"Versioning"+projectname |> List.singleton
                |PrePackage _ -> stap,"PrePackage"+projectname |> List.singleton
                |Package  _ -> stap,"Package"+projectname |> List.singleton
                |Publish  _ -> stap,"Publish"+projectname |> List.singleton 
            )    

    let generate_targets (steps:(BuildStep*string list)list) =
        steps
        |> List.iter (
            fun (step,targetnames) ->
                match step with
                |Clean dirLijst -> 
                    Target.create targetnames.Head 
                        (fun _ -> dirLijst |> List.map (fun (Directory dirnaam) -> dirnaam) |> Shell.cleanDirs )
                |Versioning releaseNotes  -> 
                    let (Version version) = releaseNotes.version
                    let (Assemblyfile assemblyfile) = releaseNotes.assemblyfile
                    Target.create targetnames.Head
                        (fun _ -> 
                            releaseNotes.attributes@[AssemblyInfo.Version version]
                            |> AssemblyInfoFile.createFSharp assemblyfile
                        )
                |PrePackage ((Projectfile projfile),(Directory dest)) ->
                    Target.create targetnames.Head (fun _ ->
                        CreateProcess.fromRawCommand "dotnet" ["publish";"--output";dest;"--configuration";"Release";projfile] |> Proc.run |> ignore
                    )
                |Package (Directory src,Version version) ->
                    Target.create targetnames.Head (fun _ ->
                        Paket.pack (fun p ->
                            { p with
                                WorkingDir = src
                                BuildConfig = "Release";
                                OutputPath = PublicDirs.Packagedirectory
                                Version = version 
                                ReleaseNotes = Fake.Tools.Git.Branches.getSHA1 "./.git/" "HEAD"
                                MinimumFromLockFile = false
                            }
                        )
                    )
                |Publish (publishfunctie,name,Version version,VersionDescription description) -> 
                    Target.create targetnames.Head (fun _ -> 
                        publishfunctie (Directory PublicDirs.Packagedirectory) (Repository PublicDirs.Packagerepository)
                        System.IO.File.AppendAllLines
                            (ReleaseNotesLocation,
                                [
                                    sprintf "Timestamp: %A" System.DateTime.Now
                                    sprintf "Assembly : %A" name
                                    sprintf "Version  : %s" version
                                    sprintf "Description: %s\n" description
                                ])
                    )
            )    

module SetTargets =
    open BuildDomain

    let publish_to_repository (Directory src) (Repository repo) = 
        // let nugetPath = System.IO.Path.Combine [|__SOURCE_DIRECTORY__;"nuget";"nuget.exe"|]
        // CreateProcess.fromRawCommand nugetPath ["init";src;repo] |> Proc.run |> ignore  
        () // to be done
    
    Target.create "CleanPackages" (fun _ -> [PublicDirs.Packagedirectory] |> Shell.cleanDirs)

    let fsTensorBuild =
        (FsTensor,
            [
                Clean [Directory "src/FsTensor/bin/";Directory "src/FsTensor/obj/";Directory "src/FsTensor/publish"]
                Versioning 
                    {
                        version = Version Versionnumbering.fsTensorVersion
                        assemblyfile = Assemblyfile "src/FsTensor/AssemblyInfo.fs"
                        attributes =
                            [
                                AssemblyInfo.Title "FsTensor"
                                AssemblyInfo.Description 
                                    ("FsTensor, Tensor manipulations and calculations as part of FsLab\n"+Versionnumbering.fsTensorDescription)
                                AssemblyInfo.Company "F# incubation space"
                            ]
                    }
                PrePackage (Projectfile "src/FsTensor/FsTensor.fsproj",Directory "src/FsTensor/publish")
                Package (Directory "src/FsTensor/",Version Versionnumbering.fsTensorVersion)
                Publish 
                    (publish_to_repository,
                        FsTensor,
                            Version Versionnumbering.fsTensorVersion,
                                VersionDescription Versionnumbering.fsTensorDescription)
            ]
        )

    do
        [
            fsTensorBuild
        ]
        |> List.iter (
            fun targetProject ->
                targetProject
                |> TargetBuilders.list_of_targets
                |> fun targets ->
                    do
                        targets 
                        |> List.map (snd >> String.concat " ")
                        |> String.concat " "
                        |> Fake.Core.Trace.trace
                    targets |> TargetBuilders.generate_targets
                )
open Fake.Core.TargetOperators

"CleanFsTensor"
    ==> "VersioningFsTensor"
    ==> "PrepackageFsTensor"
    ==> "PackageFsTensor" // not working yet
    ==> "PublishFsTensor" // not working yet

Target.runOrDefault "PrepackageFsTensor"
