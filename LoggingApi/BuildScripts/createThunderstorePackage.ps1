param ($OutDir, $TargetName, $ProjectName, $Version, $Description)
$template = Get-Content -Raw "..\manifest_template.json" | ConvertFrom-Json
$manifest = @{
    name = "$ProjectName"
    description = "$Description"
    version_number = "$Version"
    dependencies = $template.dependencies
    website_url = $template.website_url
}
$manifest | ConvertTo-Json | Out-File "$OutDir\manifest.json"

$compress = @{
    Path = "..\icon.png", "..\README.md", "..\CHANGELOG.md", "$OutDir\manifest.json"
    CompressionLevel = "Optimal"
    DestinationPath = "$OutDir\$TargetName.zip"
}
Compress-Archive -Force @compress
echo "Created package $TargetName.zip"
