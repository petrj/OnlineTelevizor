$scriptPath = $PSScriptRoot
cd $PSScriptRoot


# Export to .pfx (private key)
$pfxPassword = ConvertTo-SecureString -String "OnlineTelevizor" -Force -AsPlainText

$ca = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=OnlineTelevizor CA, 0=OnlineTelevizor, C=US" -TextExtension @("2.5.29.19={text}false") -KeyUsage DigitalSignature -KeyLength 2048 -NotAfter (Get-Date).AddMonths(33) -FriendlyName OnlineTelevizor
Export-PfxCertificate -Cert $ca -FilePath (Join-Path -Path $PSScriptRoot -ChildPath "OnlineTelevizor.pfx") -Password $pfxPassword
Export-Certificate -Cert $ca -FilePath (Join-Path -Path $PSScriptRoot -ChildPath "OnlineTelevizor.cer")
