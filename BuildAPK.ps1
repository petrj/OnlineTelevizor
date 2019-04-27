$scriptPath = $PSScriptRoot
cd $PSScriptRoot

function Create-APK
{
    [CmdletBinding()]
    [Alias()]
    [OutputType([int])]
    Param
    (
    [Parameter(Mandatory=$true,ValueFromPipeline=$true)]
	   $ProjFileName,

       [Parameter(Mandatory=$true)]
       $msbuild,

	   [ValidateSet("16", "27")]
       $MinSdkVersion,

	   [ValidateSet("16", "27")]
	   $TargetSdkVersion,


	   [ValidateSet("Release", "Debug")]
	   $Target = "Release"
    )
    Process
    {
        Write-Host ("Generateing Android build API level Min: " + $MinSdkVersion + ", target API level: " + $TargetSdkVersion)

        $projDir = [System.IO.Path]::GetDirectoryName($ProjFileName);
        $manifestFileName = [System.IO.Path]::Combine($projDir, "Properties\AndroidManifest.xml");

        Write-Host ("ManifestFileName : $manifestFileName")

		Copy-Item -Path $manifestFileName ($manifestFileName + ".backup") -Verbose
		try
		{
			[xml]$manifest = Get-content -Path $manifestFileName

			$packageName = $manifest.manifest.package;
            $version = $manifest.manifest.versionCode;

			$manifest.manifest.'uses-sdk'.minSdkVersion = "$MinSdkVersion"
			$manifest.manifest.'uses-sdk'.targetSdkVersion = "$TargetSdkVersion"

			$manifest.Save($manifestFileName)

			#Write-Host "Building APK $androidVersion ...  "

			if ($Target -eq "Release")
			{
				& $msbuild $ProjFileName /t:SignAndroidPackage /p:Configuration=Release /v:d | Out-Host
				$defaultAPKName = [System.IO.Path]::Combine($projDir, "bin\Release\",$manifest.manifest.package + "-Signed.apk");
			} else
			{
				& $msbuild $ProjFileName /t:SignAndroidPackage /p:Configuration=Debug /v:d | Out-Host
				$defaultAPKName = [System.IO.Path]::Combine($projDir, "bin\Debug\",$manifest.manifest.package + "-Signed.apk");
			}

			Copy-Item -Path ($manifestFileName + ".backup") $manifestFileName -Verbose

			if (-not (Test-Path -Path $defaultAPKName))
			{
				throw "Build failed"
			}

			$newName =  [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($defaultAPKName),$packageName+"-v"+$version+"-"+$Target+".apk")

			Rename-Item -Path $defaultAPKName -NewName $newName -Verbose

			return Get-Item $newName
		} finally
		{
			Move-Item -Path ($manifestFileName + ".backup") $manifestFileName -Force -Verbose
		}
    }
}

$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"
if (-not (Test-Path $msbuild))
{
    $msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"
}

# restore nuget packages

$url = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

if (-not (Test-Path -Path "nuget.exe"))
{
    Invoke-WebRequest -Uri $url -OutFile "nuget.exe"
}

.\nuget.exe restore .\SledovaniTVLive.sln

#Create-APK -ProjFileName "$scriptPath\SledovaniTVLive\SledovaniTVLive.Android\SledovaniTVLive.Android.csproj" -MinSdkVersion 16 -TargetSdkVersion 16 -msbuild $msbuild | Move-Item -Destination  . -Verbose
Create-APK -ProjFileName "$scriptPath\SledovaniTVLive\SledovaniTVLive.Android\SledovaniTVLive.Android.csproj" -MinSdkVersion 16 -TargetSdkVersion 27 -msbuild $msbuild | Move-Item -Destination  . -Verbose

Create-APK -ProjFileName "$scriptPath\SledovaniTVLive\SledovaniTVLive.Android\SledovaniTVLive.Android.csproj" -MinSdkVersion 16 -TargetSdkVersion 27 -msbuild $msbuild -Target "Debug" | Move-Item -Destination  . -Verbose
