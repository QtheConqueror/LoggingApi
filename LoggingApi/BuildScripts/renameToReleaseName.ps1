param ($OutDir, $TargetName, $ProjectName)
Move-Item -Force -Path "$OutDir\$TargetName.dll" -Destination "$OutDir\$ProjectName.dll"
echo "Renamed $TargetName.dll -> $ProjectName.dll"
Move-Item -Force -Path "$OutDir\$TargetName.xml" -Destination "$OutDir\$ProjectName.xml"
echo "Renamed $TargetName.xml -> $ProjectName.xml"
