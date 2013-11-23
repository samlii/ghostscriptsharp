if(-not (Get-Command Invoke-Psake -ErrorAction SilentlyContinue)) {
    Write-Warning "Psake is the build system used here but it cannot be found."
    Write-Warning "In order to install run the following from this directory:"
    Write-Warning "nuget install"
    return

}
properties {
    $configuration     = 'Release'

    $nugetPackagingDir = $pwd
    $solutionDir       = "$nugetPackagingDir/.."
    $projectDir        = "$solutionDir/GhostScriptSharp"
    $workspaceDir      = "$nugetPackagingDir/workspace"
    $buildDir          = "$projectDir/bin/$configuration"
    $gsDllDir          = "$solutionDir/ThirdParty"
}

task default -depends Build

task Build -depends toProjectDir, msBuildClean {
    Run-Msbuild ReBuild @{
        Configuration = "$configuration"
        OutDir        = $buildDir
    }
}
task CreatePackage -depends createWorkspace, copyContent, 
                            copyTools, copyBinaries, copyNuspec, pack
task toProjectDir { cd $projectDir }
task msBuildClean -depends toProjectDir {
    Run-Msbuild Clean @{
        OutDir        = $buildDir        
    }   
}
task createWorkspace {
    if(Test-Path $workspaceDir) {
        rm $workspaceDir -Recurse -Force }
    mkdir $workspaceDir
}
task toWorkspaceDir { cd $workspaceDir}
task copyContent -depends toWorkspaceDir {
    mkdir Content
    mkdir Content/Ghostscript
    cp $gsDllDir/gsdll32.dll Content/Ghostscript/
}
task copyTools -depends toWorkspaceDir {
    cp $nugetPackagingDir/Tools . -Recurse
}
task copyBinaries -depends toWorkspaceDir {
    mkdir lib
    mkdir lib/net40
    cp $buildDir/GhostscriptSharp.dll lib/net40
}
task copyNuspec {
    cp $nugetPackagingDir/GhostScriptSharp.nuspec $workspaceDir
}
task pack -depends toWorkspaceDir {
    nuget pack
}


function Run-Msbuild($target, $parameters=@{}) {
    cd $solutionDir
    $paramStrings = $parameters.GetEnumerator() |% { "/property:$($_.Key)=""$($_.Value)"""}
    $msbuild = "msbuild /nologo /target:$target $paramStrings"
    if($VerbosePreference -ne "Continue") {         #quiet mode unless explicitly told to be verbose
        $msbuild += " /verbosity:quiet" }
    Write-Host $msbuild
    exec { Invoke-Expression $msbuild }
}

