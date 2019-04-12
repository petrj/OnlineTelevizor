cd $PSScriptRoot

$convert = "C:\Program Files (x86)\ImageMagick\convert.exe"
$ResourcesFolder = "SledovaniTVPlayer\SledovaniTVPlayer.Android\Resources"
$SourceImage = "Icon.png"  # image in mipmap-xxxhdpi folder (192x192)

if ($SourceImage -eq "Icon.png")
{
    Copy-Item -Path $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -Destination $ResourcesFolder\drawable-xxxhdpi\Icon.png
    
    & $convert -size 128x128 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 128x128 $ResourcesFolder\drawable\$SourceImage
    & $convert -size 144x144 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 144x144 $ResourcesFolder\drawable-xxhdpi\$SourceImage
    & $convert -size 96x96   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 96x96   $ResourcesFolder\drawable-xhdpi\$SourceImage
    & $convert -size 72x72   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 72x72   $ResourcesFolder\drawable-hdpi\$SourceImage
}

& $convert -size 144x144 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 144x144 $ResourcesFolder\mipmap-xxhdpi\$SourceImage
& $convert -size 96x96   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 96x96   $ResourcesFolder\mipmap-xhdpi\$SourceImage
& $convert -size 72x72   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 72x72   $ResourcesFolder\mipmap-hdpi\$SourceImage
& $convert -size 48x48   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 48x48   $ResourcesFolder\mipmap-mdpi\$SourceImage
