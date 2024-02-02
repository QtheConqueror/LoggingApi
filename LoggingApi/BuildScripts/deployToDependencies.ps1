param ($OutDir, $TargetName, $ProjectName)
Copy-Item -Path "$OutDir\$TargetName.dll" -Destination "..\..\..\Dependencies\$ProjectName.dll"
echo "Coppied $TargetName.dll -> ..\..\Dependencies\$ProjectName.dll"
Copy-Item -Path "$OutDir\$TargetName.xml" -Destination "..\..\..\Dependencies\$ProjectName.xml"
echo "Coppied $TargetName.xml -> ..\..\Dependencies\$ProjectName.xml"
