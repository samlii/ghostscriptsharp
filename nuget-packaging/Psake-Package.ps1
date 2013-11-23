properties {
    $nugetPackagingDir = $pwd
    $slnDir = "$nugetPackagingDir/.."
    $projectDir = "$slnDir/GhostScriptSharp"
}
task Build -depends toProjectDir, msBuildClean
task Test
task CreatePackage
task toProjectDir {
    cd $projectDir
}
task msBuildClean -depends toProjectDir {
    Run-Msbuild Clean    
}
task createWorkspace
task copyContent
task copyTools
task copyBinaries
task copyNuspec
task pack


function Run-Msbuild($target, $parameters=@{}) {
    cd $solutionDir
    $paramStrings = $parameters.GetEnumerator() |% { "/property:$($_.Key)=""$($_.Value)"""}
    $msbuild = "msbuild /nologo /target:$target $paramStrings"
    if($VerbosePreference -ne "Continue") {         #quiet mode unless explicitly told to be verbose
        $msbuild += " /verbosity:quiet" }
    Write-Host $msbuild
    exec { Invoke-Expression $msbuild }
}
