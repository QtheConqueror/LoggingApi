param ($OutDir, $TargetName)
Copy-Item -Path "$OutDir\$TargetName.dll" -Destination "$($env:AppData)\r2modmanPlus-local\LethalCompany\profiles\Debug\BepInEx\plugins\$TargetName.dll"
echo "Coppied $TargetName.dll -> $($env:AppData)\r2modmanPlus-local\LethalCompany\profiles\Debug\BepInEx\plugins\$TargetName.dll"
