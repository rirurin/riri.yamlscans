param (
    $PublishPath = ""
)

function New-Folder {
    param (
        $folder_name
    )
    Remove-Item $folder_name -Recurse -ErrorAction SilentlyContinue
    New-Item $folder_name -ItemType Directory -ErrorAction SilentlyContinue
}

function Publish-Project {
    param (
      $project_name
    )
    dotnet restore $project_name/$project_name.csproj
    dotnet clean $project_name/$project_name.csproj
    dotnet build $project_name/$project_name.csproj -c Release -r win-x64 --self-contained false -o $PublishPath 
    Compress-Archive -Path (Get-ChildItem -Path $PublishPath* -Exclude "*.nupkg") -Force $PublishPath/Github/$project_name".zip"
    Copy-Item -Path $PublishPath/* -Include "*.nupkg" -Destination $PublishPath/Nuget -Recurse
}

New-Folder "$PublishPath/Github"
New-Folder "$PublishPath/Nuget"

Publish-Project riri.yamlscans
Publish-Project riri.yamlscans.ReloadedII