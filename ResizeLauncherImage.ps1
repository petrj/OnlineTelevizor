cd $PSScriptRoot

$convert = "C:\Program Files (x86)\ImageMagick\convert.exe"
$ResourcesFolder = "SledovaniTVPlayer\SledovaniTVPlayer.Android\Resources"
$SourceImage = "launcher_foreground.png"  # image in mipmap-xxxhdpi folder (432x432 with image cca 220x220 in center)

& $convert -size 324x324 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 324x324 $ResourcesFolder\mipmap-xxhdpi\$SourceImage
& $convert -size 216x216 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 216x216 $ResourcesFolder\mipmap-xhdpi\$SourceImage
& $convert -size 162x162 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 162x162 $ResourcesFolder\mipmap-hdpi\$SourceImage
& $convert -size 108x108 $ResourcesFolder\mipmap-xxxhdpi\$SourceImage -resize 108x108 $ResourcesFolder\mipmap-mdpi\$SourceImage
