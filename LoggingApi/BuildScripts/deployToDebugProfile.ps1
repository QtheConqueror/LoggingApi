param ($OutDir, $TargetName)
echo "Coppied $TargetName.dll -> $($env:AppData)\r2modmanPlus-local\LethalCompany\profiles\Debug\BepInEx\plugins\$TargetName.dll"
Copy-Item -Path "$OutDir\$TargetName.dll" -Destination "$($env:AppData)\r2modmanPlus-local\LethalCompany\profiles\Debug\BepInEx\plugins\$TargetName.dll"
