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
        "SledovaniTVPlayer\SledovaniTVPlayer\bin",
        "SledovaniTVPlayer\SledovaniTVPlayer\obj",
        "SledovaniTVPlayer\SledovaniTVPlayer.Android\obj",
        "SledovaniTVPlayer\SledovaniTVPlayer.Android\obj",
        ".\TestConsole\bin"
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