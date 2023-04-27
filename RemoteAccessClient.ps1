Function Send-TCPMessage 
{
    # https://riptutorial.com/powershell/example/18118/tcp-sender
    Param 
    (
            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Message,

            [Parameter(Mandatory=$true, Position=0)]
            [ValidateNotNullOrEmpty()]
            [string]
            $IP,
        
            [Parameter(Mandatory=$true, Position=1)]
            [int]
            $Port        
            
    )
    Process 
    {
        try
        {
            # Setup connection
            $IP = [System.Net.Dns]::GetHostAddresses($IP)
            $Address = [System.Net.IPAddress]::Parse($IP)
            $Socket = New-Object System.Net.Sockets.TCPClient($Address,$Port)

            # Setup stream writer
            $Stream = $Socket.GetStream()
            $Writer = New-Object System.IO.StreamWriter($Stream)

            # Write message to stream
            $Message | % {
                $Writer.WriteLine($_)
                $Writer.Flush()
            }           

            # Close connection and stream
            $Stream.Close()
            $Socket.Close()
        } catch
        {
            Write-Host $_.Exception
        }
    }
}

Function Decrypt-Message
{
    Param 
    (
            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $CipherText,

            [Parameter(Mandatory=$true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Key       
            
    )
    Process
    {
        if ($key.Length -lt 32)
        {
            $key = $key.PadRight(32, "*")
        }
        if ($key.Length -gt 32)
        {
            $key = $key.Substring(0,32)
        }

        try
        {
            $iv = [System.Byte[]]::CreateInstance([System.Byte],16)
            $buffer = [System.Convert]::FromBase64String($CipherText)

            $aes = [System.Security.Cryptography.Aes]::Create()
            $aes.Key = [System.Text.Encoding]::UTF8.GetBytes($Key)
            $aes.IV = $iv

            $decryptor = $aes.CreateDecryptor($aes.Key, $aes.IV)

            $result = $decryptor.TransformFinalBlock($buffer, 0, $buffer.Length)
            return [System.Text.Encoding]::UTF8.GetString($result)
        
        } finally
        {
            $aes.Dispose()
            $decryptor.Dispose()
        }
    }
}

Function Encrypt-Message 
{
    Param 
    (
            [Parameter(Mandatory=$true, ValueFromPipeline = $true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $PLaintText,

            [Parameter(Mandatory=$true)]
            [ValidateNotNullOrEmpty()]
            [string]
            $Key       
            
    )
    Process
    {
        $iv = [System.Byte[]]::CreateInstance([System.Byte],16)

        if ($key.Length -lt 32)
        {
            $key = $key.PadRight(32, "*")
        }
        if ($key.Length -gt 32)
        {
            $key = $key.Substring(0,32)
        }

        try
        {
            $aes = [System.Security.Cryptography.Aes]::Create()
            $plainTextBytes = [System.Text.Encoding]::UTF8.GetBytes($PLaintText);

            $aes.Key = [System.Text.Encoding]::UTF8.GetBytes($Key)
            $aes.IV = $iv

            $encryptor = $aes.CreateEncryptor($aes.Key, $aes.IV);

            $memoryStream = new-object System.IO.MemoryStream
             
            $mode = [System.Security.Cryptography.CryptoStreamMode]::Write
            $cryptoStream = new-object System.Security.Cryptography.CryptoStream($memoryStream, $encryptor, $mode)

            $cryptoStream.Write($plainTextBytes, 0, $plainTextBytes.Length)

            $cryptoStream.FlushFinalBlock();

            $array = $memoryStream.ToArray();

            return [System.Convert]::ToBase64String($array);

        } finally
        {
            $aes.Dispose()
            $memoryStream.Dispose()
            $cryptoStream.Dispose()
        }
    }
}


$msgDown = @"
{
 "securityKey":"OnlineTelevizor",
 "command":"keyDown",
 "commandArg1":"DpadDown"
}
"@

$msgEnter = @"
{
 "securityKey":"OnlineTelevizor",
 "command":"keyDown",
 "commandArg1":"Enter"
}
"@

$msg = @"
{
 "securityKey":"OnlineTelevizor",
 "command":"keyDown",
 "commandArg1":"DpadDown"
}
"@

$msg | Encrypt-Message  -Key "OnlineTelevizor"  | Send-TCPMessage -Port 49152 -IP 10.0.0.231