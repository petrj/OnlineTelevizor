cd $PSScriptRoot

$convert = "C:\Program Files (x86)\ImageMagick\convert.exe"
$ResourcesFolder = "SledovaniTVLive\SledovaniTVLive.Android\Resources"
$SourceImage = "Screens\Icon.png"


$SourceImageName = [System.IO.Path]::GetFileName($SourceImage)

& $convert -size 128x128 $SourceImage -resize 128x128 $ResourcesFolder\drawable\$SourceImageName
& $convert -size 192x192 $SourceImage -resize 192x192 $ResourcesFolder\drawable-xxxhdpi\$SourceImageName
& $convert -size 144x144 $SourceImage -resize 144x144 $ResourcesFolder\drawable-xxhdpi\$SourceImageName
& $convert -size 96x96   $SourceImage -resize 96x96   $ResourcesFolder\drawable-xhdpi\$SourceImageName
& $convert -size 72x72   $SourceImage -resize 72x72   $ResourcesFolder\drawable-hdpi\$SourceImageName

& $convert -size 192x192 $SourceImage -resize 192x192 $ResourcesFolder\mipmap-xxxhdpi\$SourceImageName
& $convert -size 144x144 $SourceImage -resize 144x144 $ResourcesFolder\mipmap-xxhdpi\$SourceImageName
& $convert -size 96x96   $SourceImage -resize 96x96   $ResourcesFolder\mipmap-xhdpi\$SourceImageName
& $convert -size 72x72   $SourceImage -resize 72x72   $ResourcesFolder\mipmap-hdpi\$SourceImageName
& $convert -size 48x48   $SourceImage -resize 48x48   $ResourcesFolder\mipmap-mdpi\$SourceImageName
