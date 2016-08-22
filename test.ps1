& %system.teamcity.build.checkoutDir%\packages\psake.4.6.0\tools\psake.ps1 %system.teamcity.build.checkoutDir%\Build.ps1 Compile
if ($psake.build_success -eq $false) { exit 1 } else { exit 0 }
