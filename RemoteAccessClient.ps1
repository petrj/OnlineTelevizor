Function Send-TCPMessage {
# https://riptutorial.com/powershell/example/18118/tcp-sender
    Param (
            [Parameter(Mandatory=$true, Position=0)]
            [ValidateNotNullOrEmpty()]
            [string]
            $EndPoint
        ,
            [Parameter(Mandatory=$true, Position=1)]
            [int]
            $Port
        ,
            [Parameter(Mandatory=$true, Position=2)]
            [string]
            $Message
    )
    Process {
        try
        {
            # Setup connection
            $IP = [System.Net.Dns]::GetHostAddresses($EndPoint)
            $Address = [System.Net.IPAddress]::Parse($IP)
            $Socket = New-Object System.Net.Sockets.TCPClient($Address,$Port)

            # Setup stream wrtier
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

#Send-TCPMessage -Port 49152 -Endpoint 10.0.0.231 -message  $msg
#Send-TCPMessage -Port 49152 -Endpoint 10.18.15.101 -message  $msg
#Send-TCPMessage -Port 49152 -Endpoint 192.168.28.242 -message  $msgDown
#Send-TCPMessage -Port 49152 -Endpoint 192.168.28.231 -message  $msgDown

$msg = @"
{
 "securityKey":"OnlineTelevizor",
 "command":"keyDown",
 "commandArg1":"DpadDown"
}
"@

Send-TCPMessage -Port 49152 -Endpoint 192.168.29.253 -message  $msg