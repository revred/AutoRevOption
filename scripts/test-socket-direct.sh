#!/usr/bin/env bash
# Direct socket test to see Gateway response (converted from PowerShell)

echo "Direct Socket Test - IBKR Gateway"
echo "=================================="
echo ""

powershell.exe -Command "
\$client = New-Object System.Net.Sockets.TcpClient
\$client.Connect('127.0.0.1', 4001)

Write-Host \"Connected: \$(\$client.Connected)\"
Write-Host \"\"

\$stream = \$client.GetStream()
\$stream.ReadTimeout = 3000

# Send API handshake
\$handshake = [System.Text.Encoding]::ASCII.GetBytes('API=9.72' + [char]0)
Write-Host \"Sending \$(\$handshake.Length) bytes: API=9.72[NULL]\"
\$stream.Write(\$handshake, 0, \$handshake.Length)
\$stream.Flush()

# Wait and read
Start-Sleep -Milliseconds 500
Write-Host \"Bytes available: \$(\$stream.DataAvailable)\"
Write-Host \"\"

if (\$stream.DataAvailable) {
    \$buffer = New-Object byte[] 1024
    \$bytesRead = \$stream.Read(\$buffer, 0, \$buffer.Length)
    Write-Host \"✅ SUCCESS - Read \$bytesRead bytes:\"
    Write-Host \"HEX: \$([BitConverter]::ToString(\$buffer, 0, \$bytesRead))\"
    \$ascii = [System.Text.Encoding]::ASCII.GetString(\$buffer, 0, \$bytesRead)
    \$ascii = \$ascii -replace '[\x00]', '[NULL]'
    Write-Host \"ASCII: \$ascii\"
    Write-Host \"\"
    Write-Host \"✅ Gateway API is responding!\"
    \$client.Close()
    exit 0
} else {
    Write-Host \"❌ TIMEOUT - No response from Gateway\"
    Write-Host \"\"
    Write-Host \"Gateway accepted TCP connection but sent zero bytes.\"
    Write-Host \"This indicates Gateway is rejecting the API handshake.\"
    \$client.Close()
    exit 1
}
"
