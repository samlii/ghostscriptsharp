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

task default -depends Build,CreatePackage

task Build -depends msBuildClean {
    cd $projectDir
    Run-Msbuild ReBuild @{
        Configuration = "$configuration"
        OutDir        = $buildDir
    }
}
task CreatePackage -depends createWorkspace, copyGhostscriptDll, 
                            copyTools, copyBinaries, copyNuspec, pack
task msBuildClean {
    cd $projectDir
    Run-Msbuild Clean @{
        OutDir        = $buildDir        
    }   
}
task createWorkspace {
    if(Test-Path $workspaceDir) {
        rm $workspaceDir -Recurse -Force }
    mkdir $workspaceDir
}
task copyGhostscriptDll -depends createWorkspace,copyTools {
    cd $workspaceDir
    cp $gsDllDir/gsdll32.dll Tools/
}
task copyTools -depends createWorkspace {
    cd $workspaceDir
    cp $nugetPackagingDir/Tools . -Recurse
}
task copyBinaries -depends createWorkspace {
    cd $workspaceDir
    mkdir lib
    mkdir lib/net40
    cp $buildDir/GhostscriptSharp.dll lib/net40
}
task copyNuspec {
    cp $nugetPackagingDir/GhostScriptSharp.nuspec $workspaceDir
}
task pack -depends createWorkspace {
    cd $workspaceDir
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

