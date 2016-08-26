Framework "4.5.1"
$ErrorActionPreference = "stop"

properties {
    # directories
	$base_dir  = resolve-path .
    $build_dir = "$base_dir\build"
    $publish_dir = "$build_dir\publish"

    # files
    $global_assembly_info_file = "$base_dir\CommonAssemblyInfo.cs"
	
	# build/deployment settings
	$solution_file = "$base_dir\Breakfastbot.sln"
    $build_configuration = "Release"
    $build_target = "Rebuild"
    $build_verbosity = "diag"
    $version_major = 1
    $version_minor = 0
}

tasksetup {
    if($env:TEAMCITY_PROJECT_NAME -ne $null){
        # detect if this script is being run in TeamCity
        write-output "##teamcity[progressMessage '$($psake.context.Peek().currentTaskName)']"
    }
}

task Compile -depends UpdateGlobalAssemblyInfo {
    write-host "Solution: $solution_file"
    write-host "Configuration: $build_configuration"
    write-host "Target $build_target"
    write-host "Verbosity: $build_verbosity"
    write-host "Output directory: $build_dir"
	
	exec { msbuild "$solution_file" /t:$build_target /p:configuration=$build_configuration /p:VisualStudioVersion=12.0 /p:outdir="$build_dir" /verbosity:$build_verbosity /m /p:BuildInParallel=true /p:TrackFileAccess=false }
}

 task UpdateGlobalAssemblyInfo -depends GenerateVersionNumber, GetCommitHash {
    $global_assembly_info = (get-content $global_assembly_info_file) `
                            | foreach-object { $_ -replace "AssemblyConfiguration\((.+?`)\)"       , "AssemblyConfiguration(`"$build_configuration`")" } `
                            | foreach-object { $_ -replace "AssemblyVersion\((.+?`)\)"             , "AssemblyVersion(`"$version`")" } `
                            | foreach-object { $_ -replace "AssemblyFileVersion\((.+?`)\)"         , "AssemblyFileVersion(`"$version`")" } `
                            | foreach-object { $_ -replace "AssemblyInformationalVersion\((.+?`)\)", "AssemblyInformationalVersion(`"$version-$short_hash`")" }
    
    $global_assembly_info | write-host
    $global_assembly_info | set-content $global_assembly_info_file -encoding UTF8
}

task GenerateVersionNumber {
    $build_number = if("$env:BUILD_NUMBER".length -gt 0) { $env:BUILD_NUMBER } else { 0 }
    set-variable build_number $build_number -scope global
    set-variable version "$version_major.$version_minor.$build_number" -scope global
    
    write-host "Major version: $version_major"
    write-host "Minor version: $version_minor"
    write-host "Build number: $build_number"
    write-host "VERSION: $version"
}

task GetCommitHash {
    exec { git rev-parse --short HEAD } | set-variable short_hash -scope global
    write-host "Git commit hash: $short_hash"
}

function exec([scriptblock]$cmd, [bool]$throwOnError = $true, [string]$errorMessage = "`n[ERROR] Failed to execute command `"$cmd`"`n") {
    try {
        & $cmd
    } catch {
        if ($LastExitCode -ne 0 -and $throwOnError) {
            write-host $errorMessage -foregroundColor red
            write-host "`tMessage: $($_.Exception.Message)" -foregroundColor red
            write-host "`tType: $($_.Exception.GetType())`n" -foregroundColor red
            throw
        }
    }

    if (!$throwOnError) { return ($LastExitCode -eq 0) }
}
