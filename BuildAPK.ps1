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

	   [ValidateSet("16", "25")]
       $MinSdkVersion,

	   [ValidateSet("16", "25")]
	   $TargetSdkVersion
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

			$dateTimeSuffix = [DateTime]::Now.ToString("yyyy-MM-dd--HHmm")

			$manifest.manifest.'uses-sdk'.minSdkVersion = "$MinSdkVersion"
			$manifest.manifest.'uses-sdk'.targetSdkVersion = "$TargetSdkVersion"

			$manifest.Save($manifestFileName)

			#Write-Host "Building APK $androidVersion ...  "
			& $msbuild $ProjFileName /t:SignAndroidPackage /p:Configuration=Release /v:d | Out-Host

			$defaultAPKName = [System.IO.Path]::Combine($projDir, "bin\Release\",$manifest.manifest.package + "-Signed.apk");

			Copy-Item -Path ($manifestFileName + ".backup") $manifestFileName -Verbose

			if (-not (Test-Path -Path $defaultAPKName))
			{
				throw "Build failed"
			}

			$newName =  [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($defaultAPKName),$manifest.manifest.application.label+"-"+$dateTimeSuffix+"-mapi" + $MinSdkVersion+"-tapi"+$TargetSdkVersion+".apk")

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

#./Clean.ps1

Create-APK -ProjFileName "$scriptPath\SledovaniTVPlayer\SledovaniTVPlayer.Android\SledovaniTVPlayer.Android.csproj" -MinSdkVersion 16 -TargetSdkVersion 16 -msbuild $msbuild | Move-Item -Destination  . -Verbose
Create-APK -ProjFileName "$scriptPath\SledovaniTVPlayer\SledovaniTVPlayer.Android\SledovaniTVPlayer.Android.csproj" -MinSdkVersion 25 -TargetSdkVersion 25 -msbuild $msbuild | Move-Item -Destination  . -Verbose

#./Clean.ps1

