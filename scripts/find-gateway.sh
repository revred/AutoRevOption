#!/usr/bin/env bash
set -euo pipefail

echo "=== Searching for Client Portal Gateway Installation ==="
echo ""

# Common installation paths
COMMON_PATHS=(
  "/c/IBKR/clientportal"
  "/c/Program Files/IBKR/clientportal"
  "/c/Program Files (x86)/IBKR/clientportal"
  "$HOME/IBKR/clientportal"
  "/c/Users/$USER/IBKR/clientportal"
)

echo "Checking common installation paths..."
for path in "${COMMON_PATHS[@]}"; do
  if [ -d "$path" ]; then
    echo "✅ Found: $path"
    echo ""
    echo "Contents:"
    ls -la "$path" || true
    echo ""

    # Check for startup scripts
    if [ -f "$path/bin/run.sh" ]; then
      echo "Startup script (Linux/Mac): $path/bin/run.sh"
    fi
    if [ -f "$path/bin/run.bat" ]; then
      echo "Startup script (Windows): $path/bin/run.bat"
    fi
    if [ -f "$path/clientportal.gw/bin/run.sh" ]; then
      echo "Startup script (Linux/Mac): $path/clientportal.gw/bin/run.sh"
    fi
    if [ -f "$path/clientportal.gw/bin/run.bat" ]; then
      echo "Startup script (Windows): $path/clientportal.gw/bin/run.bat"
    fi
  fi
done

echo ""
echo "Searching entire C: drive for 'clientportal.gw'..."
find /c -type d -name "clientportal.gw" 2>/dev/null | head -10 || echo "Not found"

echo ""
echo "Checking if Gateway is already running..."
netstat -an | grep :5000 || echo "Port 5000 not listening"

echo ""
echo "Checking for Java processes (Gateway requires Java)..."
tasklist | grep -i java || echo "No Java processes found"

echo ""
echo "=== Installation Instructions ==="
echo "If Gateway not found, download from:"
echo "  https://www.interactivebrokers.com/en/trading/ibgateway-latest.php"
echo ""
echo "Or use Client Portal Gateway (standalone):"
echo "  Download clientportal.gw.zip from Customer Portal > Technology > API Software"
echo "  Extract to C:\\IBKR\\clientportal\\"
echo "  Start: C:\\IBKR\\clientportal\\bin\\run.bat"
