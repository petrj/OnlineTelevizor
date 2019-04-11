cd $PSScriptRoot

$convert = "C:\Program Files (x86)\ImageMagick\convert.exe"
$ResourcesFolder = "SledovaniTVPlayer\SledovaniTVPlayer.Android\Resources"
$SourceImage = "Icon.png"  # image in mipmap-xxxhdpi folder (192x192)

& $convert -size 144x144 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 144x144 $ResourcesFolder\mipmap-xxhdpi\$SourceImage
& $convert -size 96x96   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 96x96   $ResourcesFolder\mipmap-xhdpi\$SourceImage
& $convert -size 72x72   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 72x72   $ResourcesFolder\mipmap-hdpi\$SourceImage
& $convert -size 48x48   $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 48x48   $ResourcesFolder\mipmap-mdpi\$SourceImage
