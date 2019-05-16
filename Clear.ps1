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
        "OnlineTelevizor\OnlineTelevizor\bin",
        "OnlineTelevizor\OnlineTelevizor\obj",
        "OnlineTelevizor\OnlineTelevizor.Android\bin",
        "OnlineTelevizor\OnlineTelevizor.Android\obj",
		"OnlineTelevizor\OnlineTelevizor.UWP\bin",
		"OnlineTelevizor\OnlineTelevizor.UWP\obj",
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
