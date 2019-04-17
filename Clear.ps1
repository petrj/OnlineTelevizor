$scriptPath = $PSScriptRoot
cd $PSScriptRoot

foreach ($folder in `
    @(
        ".vs",
        "LoggerService\bin",
        "LoggerService\obj",
        "packages",
        "SledovaniTVApi\bin",
        "SledovaniTVApi\obj",
        "SledovaniTVLive\SledovaniTVLive\bin",
        "SledovaniTVLive\SledovaniTVLive\obj",
        "SledovaniTVLive\SledovaniTVLive.Android\bin",
        "SledovaniTVLive\SledovaniTVLive.Android\obj",
        ".\TestConsole\bin",
		".\TestConsole\obj"
     ))
{
    $fullPath = [System.IO.Path]::Combine($scriptPath,$folder)
    if (-not $fullPath.EndsWith("\"))
    {
            $fullPath += "\"
    }

    if (Test-Path -Path $fullPath)
    {
        Remove-Item -Path $fullPath -Recurse -Force -Verbose
    }
}
